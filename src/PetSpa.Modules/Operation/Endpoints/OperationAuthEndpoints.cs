using System.Globalization;
using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using PetSpa.Modules.Operation.Application.Services;
using PetSpa.Modules.Operation.Endpoints.Requests;
using PetSpa.Modules.Operation.Endpoints.Responses;
using PetSpa.SharedKernel.Application.Abstractions;

namespace PetSpa.Modules.Operation.Endpoints;

public static class OperationAuthEndpoints
{
    public static IEndpointRouteBuilder MapCustomerAuthEndpoints(
        this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/staff/login", LoginAsync)
            .WithTags("Staffs")
            .WithName("StaffLogin")
            .AllowAnonymous();

        return app;
    }

    private static async Task<IResult> LoginAsync(
        StaffLoginRequest request,
        IValidator<StaffLoginRequest> validator,
        StaffLoginService loginService,
        ITokenService tokenService,
        CancellationToken ct)
    {
        // 1. Kiểm tra dữ liệu đầu vào.
        var validation = await validator.ValidateAsync(request, ct);

        if (!validation.IsValid)
        {
            return Results.ValidationProblem(validation.ToDictionary());
        }

        // 2. Kiểm tra tài khoản và mật khẩu.
        var staff = await loginService.LoginAsync(
            request.Phone,
            request.Password,
            ct);

        if (staff is null)
        {
            return Results.Unauthorized();
        }

        // 3. Tạo token từ thông tin đã xác thực.
        var staffId = staff.Id.ToString(
            CultureInfo.InvariantCulture);

        var token = tokenService.Generate(
        [
            new Claim("sub", $"staff:{staffId}"),
            new Claim("account_type", "staff"),
            new Claim("account_role", staff.Role.ToString()),
            new Claim("staff_id", staffId)
        ]);

        // 4. Trả response cho client.
        return Results.Ok(new StaffLoginResponse(
            token.AccessToken,
            "Bearer",
            token.ExpiresAtUtc,
            staff));
    }
}

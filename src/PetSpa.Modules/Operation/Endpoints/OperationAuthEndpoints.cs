// Định nghĩa hoặc hỗ trợ các endpoint OperationAuthEndpoints.
using System.Globalization;
using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using PetSpa.Modules.Operation.Application.Services;
using PetSpa.Modules.Operation.Endpoints.Requests;
using PetSpa.SharedKernel.Application.Abstractions;
using PetSpa.SharedKernel.Application.Authentication;
using PetSpa.SharedKernel.Application.Dtos;

namespace PetSpa.Modules.Operation.Endpoints;

public static class OperationAuthEndpoints
{
    // Đăng ký endpoint đăng nhập nhân viên.
    public static IEndpointRouteBuilder MapOperationAuthEndpoints(
        this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/auth/staff/login", LoginAsync)
            .WithTags("Staffs")
            .WithName("StaffLogin")
            .Produces<LoginResponse>()
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status401Unauthorized)
            .AllowAnonymous();

        return app;
    }

    // Xác thực thông tin đăng nhập.
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

        const string accountType = AccountTypes.Staff;
        var token = tokenService.Generate(
        [
            new Claim(AuthClaimTypes.Subject, $"{accountType}:{staffId}"),
            new Claim(AuthClaimTypes.Role, staff.Role),
            new Claim(AuthClaimTypes.AccountType, accountType),
            new Claim(AuthClaimTypes.StaffId, staffId)
        ]);

        // 4. Trả response cho client.
        return Results.Ok(new LoginResponse(
            token.AccessToken,
            "Bearer",
            token.ExpiresAtUtc,
            staff.Role,
            new LoginUser(staff.Id, staff.FullName, staff.Phone)));
    }
}

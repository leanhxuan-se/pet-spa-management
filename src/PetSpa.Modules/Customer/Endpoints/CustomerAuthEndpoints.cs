using System.Globalization;
using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using PetSpa.Modules.Customer.Application.Services;
using PetSpa.Modules.Customer.Endpoints.Requests;
using PetSpa.SharedKernel.Application.Abstractions;
using PetSpa.SharedKernel.Application.Authentication;
using PetSpa.SharedKernel.Application.Dtos;

namespace PetSpa.Modules.Customer.Endpoints;

public static class CustomerAuthEndpoints
{
    public static IEndpointRouteBuilder MapCustomerAuthEndpoints(
        this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/auth/customer/login", LoginAsync)
            .WithTags("Customers")
            .WithName("CustomerLogin")
            .Produces<LoginResponse>()
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status401Unauthorized)
            .AllowAnonymous();

        return app;
    }

    private static async Task<IResult> LoginAsync(
        CustomerLoginRequest request,
        IValidator<CustomerLoginRequest> validator,
        CustomerLoginService loginService,
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
        var customer = await loginService.LoginAsync(
            request.Phone,
            request.Password,
            ct);

        if (customer is null)
        {
            return Results.Unauthorized();
        }

        // 3. Tạo token từ thông tin đã xác thực.
        var customerId = customer.Id.ToString(
            CultureInfo.InvariantCulture);

        const string role = AccountRoles.Customer;
        const string accountType = AccountTypes.Customer;
        var token = tokenService.Generate(
        [
            new Claim(AuthClaimTypes.Subject, $"{accountType}:{customerId}"),
            new Claim(AuthClaimTypes.Role, role),
            new Claim(AuthClaimTypes.AccountType, accountType),
            new Claim(AuthClaimTypes.CustomerId, customerId)
        ]);

        // 4. Trả response cho client.
        return Results.Ok(new LoginResponse(
            token.AccessToken,
            "Bearer",
            token.ExpiresAtUtc,
            role,
            new LoginUser(customer.Id, customer.FullName, customer.Phone)));
    }
}

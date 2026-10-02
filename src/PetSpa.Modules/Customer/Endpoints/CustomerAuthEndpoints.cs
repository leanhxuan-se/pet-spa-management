using System.Globalization;
using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using PetSpa.Modules.Customer.Application.Services;
using PetSpa.Modules.Customer.Endpoints.Requests;
using PetSpa.Modules.Customer.Endpoints.Responses;
using PetSpa.SharedKernel.Application.Abstractions;

namespace PetSpa.Modules.Customer.Endpoints;

public static class CustomerAuthEndpoints
{
    public static IEndpointRouteBuilder MapCustomerAuthEndpoints(
        this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/customer/login", LoginAsync)
            .WithTags("Customers")
            .WithName("CustomerLogin")
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

        var token = tokenService.Generate(
        [
            new Claim("sub", $"customer:{customerId}"),
            new Claim("account_type", "customer"),
            new Claim("customer_id", customerId)
        ]);

        // 4. Trả response cho client.
        return Results.Ok(new CustomerLoginResponse(
            token.AccessToken,
            "Bearer",
            token.ExpiresAtUtc,
            customer));
    }
}

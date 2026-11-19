// Ánh xạ và kiểm tra dữ liệu đầu vào của hai route đăng nhập.
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using PetSpa.Modules.Auth.Application;
using PetSpa.Modules.Auth.Contracts;
using PetSpa.SharedKernel.Application.Authentication;

namespace PetSpa.Modules.Auth.Endpoints;

internal static class AuthEndpoints
{
    // Đăng ký route đăng nhập riêng cho khách hàng và nhân viên.
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        // Ánh xạ mỗi route tới kho tài khoản tương ứng và mô tả phản hồi.
        DescribeLogin(app.MapPost("/api/auth/customer/login",
            (LoginRequest request, IValidator<LoginRequest> validator, LoginService service,
                [FromKeyedServices(AccountTypes.Customer)] ILegacyLoginStore store, CancellationToken ct) =>
                LoginAsync(request, validator, service, store, AccountTypes.Customer, ct)),
            "CustomerLogin", "Customers");

        DescribeLogin(app.MapPost("/api/auth/staff/login",
            (LoginRequest request, IValidator<LoginRequest> validator, LoginService service,
                [FromKeyedServices(AccountTypes.Staff)] ILegacyLoginStore store, CancellationToken ct) =>
                LoginAsync(request, validator, service, store, AccountTypes.Staff, ct)),
            "StaffLogin", "Staffs");

        return app;
    }

    // Giữ nguyên tên, tag và response để các client dùng chung một hợp đồng.
    private static void DescribeLogin(RouteHandlerBuilder route, string name, string tag)
    {
        route.WithName(name)
            .WithTags(tag)
            .Produces<LoginResponse>()
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status401Unauthorized)
            .AllowAnonymous();
    }

    // Kiểm tra yêu cầu, xác thực tài khoản và trả phản hồi đăng nhập.
    private static async Task<IResult> LoginAsync(
        LoginRequest request, IValidator<LoginRequest> validator, LoginService service,
        ILegacyLoginStore store, string accountType, CancellationToken ct)
    {
        // 1. Kiểm tra dữ liệu yêu cầu.
        var validation = await validator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return Results.ValidationProblem(validation.ToDictionary());

        // 2. Gọi dịch vụ đăng nhập và chuyển kết quả sang HTTP.
        var response = await service.LoginAsync(accountType, store, request.Phone, request.Password, ct);
        return response is null ? Results.Unauthorized() : Results.Ok(response);
    }
}

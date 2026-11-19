// Định nghĩa hoặc hỗ trợ các endpoint CustomerEndpoints.
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using PetSpa.Modules.Customer.Application.Abstractions;
using PetSpa.Modules.Customer.Endpoints.Requests;
using PetSpa.Modules.Customer.Endpoints.Responses;
using System.Security.Claims;
using PetSpa.SharedKernel.Application.Abstractions;
using PetSpa.SharedKernel.Application.Authentication;

namespace PetSpa.Modules.Customer.Endpoints;

public static class CustomerEndpoints
{
    // Đăng ký endpoint khách hàng.
    public static IEndpointRouteBuilder MapCustomerEndpoint(this IEndpointRouteBuilder app)
    {
        // 1. Tạo nhóm endpoint và cấu hình quyền truy cập.
        var group = app.MapGroup("/api/customers").WithTags("Customers"); // gom nhóm các endpoint liên quan đến customer lại ở swagger

        // 2. Đăng ký các đường dẫn và phương thức HTTP.
        group.MapGet("", GetCustomersAsync).WithName("GetCustomers")
            .RequireAuthorization(AuthorizationPolicies.CustomerManagement);
        group.MapGet("/{id:long}", GetCustomerByIdAsync).WithName("GetCustomerById")
            .RequireAuthorization(AuthorizationPolicies.AuthenticatedAccount);
        group.MapPost("/", CreateCustomerAsync).WithName("CreateCustomer")
            .RequireAuthorization(AuthorizationPolicies.CustomerManagement);
        group.MapPut("/{id:long}", UpdateCustomerAsync).WithName("UpdateCustomer")
            .RequireAuthorization(AuthorizationPolicies.AuthenticatedAccount);

        return app;
    }

    public static async Task<IResult> GetCustomersAsync(
        ICustomerService customerService,
        CancellationToken ct
    )
    {
        var Customer = await customerService.GetAsync(ct);

        if (Customer.Status == ResultStatus.NotFound)
        {
            return Results.NotFound(new { Message = $"Không tìm thấy khách hàng nào" });
        }

        return Customer.ToHttpResult();
    }

    public static async Task<IResult> GetCustomerByIdAsync(
        long id,
        ClaimsPrincipal currentUser,
        ICustomerService customerService,
        CancellationToken ct
    )
    {
        // 1. Kiểm tra quyền truy cập của tài khoản.
        if (!currentUser.CanAccessCustomer(id))
            return Results.Forbid();

        // 2. Lấy thông tin khách hàng.
        var Customer = await customerService.GetByIdAsync(id, ct);

        if (Customer.Status == ResultStatus.NotFound)
        {
            return Results.NotFound(new { Message = $"Không tìm thấy khách hàng với Id {id}" });
        }

        // 3. Trả kết quả xử lý.
        return Customer.ToHttpResult();
    }

    // Tạo khách hàng mới.
    public static async Task<IResult> CreateCustomerAsync(
        CreateCustomerRequest req,
        ICustomerService customerService,
        CancellationToken ct)
    {
        // 1. Gọi dịch vụ xử lý thông tin khách hàng.
        var customer = await customerService.CreateAsync(req, ct);

        // 2. Trả kết quả xử lý.
        return customer.ToHttpResult(
            "GetCustomerById",
            p => new { id = p?.Id }
        );

    }

    // Cập nhật thông tin khách hàng.
    public static async Task<IResult> UpdateCustomerAsync(
        long id,
        UpdateCustomerRequest req,
        ClaimsPrincipal currentUser,
        ICustomerService customerService,
        CancellationToken ct)
    {
        // 1. Kiểm tra quyền truy cập của tài khoản.
        if (!currentUser.CanAccessCustomer(id))
            return Results.Forbid();

        // 2. Gọi dịch vụ xử lý thông tin khách hàng.
        var customer = await customerService.UpdateAsync(id, req, ct);

        // 3. Trả kết quả xử lý.
        return customer.ToHttpResult(
            "GetCustomerById",
            p => new { id = p?.Id }
        );
    }
}

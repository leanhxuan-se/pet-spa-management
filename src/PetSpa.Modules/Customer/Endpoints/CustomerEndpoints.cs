using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using PetSpa.Modules.Customer.Application.Abstractions;
using PetSpa.Modules.Customer.Endpoints.Requests;
using PetSpa.Modules.Customer.Endpoints.Responses;
using System.Security.Claims;
using PetSpa.SharedKernel.Application.Abstractions;

namespace PetSpa.Modules.Customer.Endpoints;

public static class CustomerEndpoints
{
    public static IEndpointRouteBuilder MapCustomerEndpoint(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/customer").WithTags("Customers"); // gom nhóm các endpoint liên quan đến customer lại ở swagger

        group.MapGet("/{id:long}", GetCustomerByIdAsync).WithName("GetCustomerById").RequireAuthorization();
        group.MapPost("/create", CreateCustomerAsync).WithName("CreateCustomer").RequireAuthorization();
        group.MapPut("/update/{id:long}", UpdateCustomerAsync).WithName("UpdateCustomer").RequireAuthorization();

        return app;
    }

    public static async Task<IResult> GetCustomerByIdAsync(
        long id,
        ClaimsPrincipal currentUser,
        ICustomerService customerService,
        CancellationToken ct
    )
    {
        //Phần quyền truy cập người dùng hiện tại, ko là recep hoặc manager và ko phải là chính khách hàng đó thì không được phép truy cập 
        var currentUserId = Convert.ToInt64(currentUser.FindFirst("customer_id")?.Value);
        var currentRole = currentUser.FindFirst("account_role")?.Value;

        if (currentRole != "MANAGER" && currentRole != "RECEPTIONIST" && currentUserId != id)
        {
            return Results.Forbid();
        }

        var Customer = await customerService.GetByIdAsync(id, ct);

        if (Customer.Status == ResultStatus.NotFound)
        {
            return Results.NotFound(new { Message = $"Không tìm thấy khách hàng với Id {id}" });
        }

        return Results.Ok<CustomerResponse>(Customer.Value);
    }

    public static async Task<IResult> CreateCustomerAsync(
        CreateCustomerRequest req,
        ClaimsPrincipal currentUser,
        ICustomerService customerService,
        CancellationToken ct)
    {
        //Phần quyền truy cập người dùng hiện tại, không phải recep tionist hoặc manager thì không được phép tạo khách hàng
        var currentRole =  currentUser.FindFirst("account_role")?.Value;

        if(currentRole != "MANAGER" && currentRole != "RECEPTIONIST")
        {
            return Results.Forbid();
        }

        var customer = await customerService.CreateAsync(req, ct);
        
        switch(customer.Status)
        {
            case ResultStatus.Success:
                return Results.CreatedAtRoute("GetCustomerById", new { id = customer?.Value?.Id }, customer?.Value);
            case ResultStatus.ValidationFailed:
                return Results.ValidationProblem(customer?.Errors);
            default:
                return Results.BadRequest(new { Message = "Không thể tạo khách hàng" });
        }

    }

    public static async Task<IResult> UpdateCustomerAsync(
        long id,
        UpdateCustomerRequest req,
        ClaimsPrincipal currentUser,
        ICustomerService customerService,
        CancellationToken ct)
    {
        //Phần quyền truy cập người dùng hiện tại, ko là recep hoặc manager và ko phải là chính khách hàng đó thì không được phép cập nhật
        var currentUserId = Convert.ToInt64(currentUser.FindFirst("customer_id")?.Value);
        var currentRole = currentUser.FindFirst("account_role")?.Value;

        if (currentRole != "MANAGER" && currentRole != "RECEPTIONIST" && currentUserId != id)
        {
            return Results.Forbid();
        }

        var customer = await customerService.UpdateAsync(id, req, ct);

        switch (customer.Status)
        {
            case ResultStatus.Success:
                return Results.CreatedAtRoute("GetCustomerById", new { id = customer?.Value?.Id }, customer?.Value);
            case ResultStatus.ValidationFailed:
                return Results.ValidationProblem(customer?.Errors);
            default:
                return Results.BadRequest(new { errors = customer.Errors });
        }

    }
}


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
        group.MapPut("/update", UpdateCustomerAsync).WithName("UpdateCustomer").RequireAuthorization();

        return app;
    }

    public static async Task<IResult> GetCustomerByIdAsync(
        long id,
        ICustomerService customerService,
        CancellationToken ct
    )
    {
        var Customer = await customerService.GetByIdAsync(id, ct);

        if (Customer.Status == ResultStatus.NotFound)
        {
            return Results.NotFound(new { Message = $"Không tìm thấy khách hàng với Id {id}" });
        }

        return Results.Ok<CustomerResponse>(Customer.Value);
    }

    public static async Task<IResult> CreateCustomerAsync(
        CreateCustomerRequest req,
        ICustomerService customerService,
        CancellationToken ct)
    {
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
        UpdateCustomerRequest req,
        ICustomerService customerService,
        ClaimsPrincipal currentUser,
        CancellationToken ct)
    { 
        var currentUserId = currentUser.FindFirst("customerId")?.Value;

        var customer = await customerService.UpdateAsync(Convert.ToInt64(currentUserId), req, ct);

        switch (customer.Status)
        {
            case ResultStatus.Success:
                return Results.CreatedAtRoute("GetCustomerById", new { id = customer?.Value?.Id }, customer?.Value);
            case ResultStatus.ValidationFailed:
                return Results.ValidationProblem(customer?.Errors);
            default:
                return Results.BadRequest(new { Message = "Không thể tạo khách hàng" });
        }

    }
}


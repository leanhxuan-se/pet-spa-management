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
    public static IEndpointRouteBuilder MapCustomerEndpoint(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/customers").WithTags("Customers"); // gom nhóm các endpoint liên quan đến customer lại ở swagger

        group.MapGet("/{id:long}", GetCustomerByIdAsync).WithName("GetCustomerById")
            .RequireAuthorization(AuthorizationPolicies.AuthenticatedAccount);
        group.MapPost("/", CreateCustomerAsync).WithName("CreateCustomer")
            .RequireAuthorization(AuthorizationPolicies.CustomerManagement);
        group.MapPut("/{id:long}", UpdateCustomerAsync).WithName("UpdateCustomer")
            .RequireAuthorization(AuthorizationPolicies.AuthenticatedAccount);

        return app;     
    }

    public static async Task<IResult> GetCustomerByIdAsync(
        long id,
        ClaimsPrincipal currentUser,
        ICustomerService customerService,
        CancellationToken ct
    )
    {
        if (!currentUser.CanAccessCustomer(id))
            return Results.Forbid();

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
        
        return customer.ToHttpResult(
            "GetCustomerById",
            p => new { id = p?.Id }
        );

    }

    public static async Task<IResult> UpdateCustomerAsync(
        long id,
        UpdateCustomerRequest req,
        ClaimsPrincipal currentUser,
        ICustomerService customerService,
        CancellationToken ct)
    {
        if (!currentUser.CanAccessCustomer(id))
            return Results.Forbid();

        var customer = await customerService.UpdateAsync(id, req, ct);

        return customer.ToHttpResult(
            "GetCustomerById",
            p => new { id = p?.Id }
        );
    }
}


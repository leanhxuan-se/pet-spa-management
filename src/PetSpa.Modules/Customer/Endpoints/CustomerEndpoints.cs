using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using PetSpa.Modules.Customer.Domain.Entities;
using PetSpa.Modules.Customer.Endpoints.Requests;
using PetSpa.Modules.Customer.Endpoints.Responses;
using PetSpa.Modules.Customer.Infrastructure.Persistence;

using CustomerTable = PetSpa.Modules.Customer.Domain.Entities.Customer;

namespace PetSpa.Modules.Customer.Endpoints;

public static class CustomerEndpoints
{
    public static IEndpointRouteBuilder MapCustomerEndpoint(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/customer").WithTags("Customers"); // withtag to group these Endpoint handler in a swagger called "Customers"

        group.MapPost("/create", CreateCustomerAsync).WithName("CreateCustomer");

        return app;
    }

    public static async Task<IResult> CreateCustomerAsync(
        CreateCustomerRequest req, 
        CustomerDbContext db,
        IValidator<CreateCustomerRequest> validator,
        CancellationToken ct)
    {
        var validate = await validator.ValidateAsync(req, ct);

        if(!validate.IsValid)
        {
            return Results.ValidationProblem(validate.ToDictionary());
        }

        var Customer = new CustomerTable
        {
            FullName = req.FullName.Trim(),
            Phone = req.Phone.Trim(),
            Email = string.IsNullOrWhiteSpace(req.Email) ? null : req.Email.Trim(),
            Gender = req.Gender.HasValue ? req.Gender : null,
            DateOfBirth = req.DateOfBirth.HasValue ? req.DateOfBirth : null

        };

        db.Customers.Add(Customer);
        await db.SaveChangesAsync(ct);
        var response = new CustomerResponse(
            Customer.Id,
            Customer.FullName,
            Customer?.Gender,
            Customer?.DateOfBirth,  
            Customer?.Email,
            Customer.Phone,
            Customer?.Note,
            Customer?.Status.ToString()
        );

        return Results.Created();
    }
}

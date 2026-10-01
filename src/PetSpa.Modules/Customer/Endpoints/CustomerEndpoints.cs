using FluentValidation;
using Mapster;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using PetSpa.Modules.Customer.Application.Validators;
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

        group.MapGet("{id:long}", GetCustomerByIdAsync).WithName("GetCustomerById");
        group.MapPost("/create", CreateCustomerAsync).WithName("CreateCustomer");
        group.MapPut("/update/{id:long}/", UpdateCustomerAsync).WithName("UpdateCustomer");

        return app;
    }

    public static async Task<IResult> GetCustomerByIdAsync(
        long id,
        CustomerDbContext db,
        CancellationToken ct
    )
    {
                                                                 // Query database and take matched field to CustomerResponse
        var Customer = await db.Customers.Where(x => x.Id == id).ProjectToType<CustomerResponse>().FirstOrDefaultAsync(ct);

        if(Customer is null)
        {
            return Results.NotFound(new {Message = $"Không tìm thấy khách hàng với Id {id}" });
        }

        return Results.Ok<CustomerResponse>(Customer);
    }

    public static async Task<IResult> CreateCustomerAsync(
        CreateCustomerRequest req,
        CustomerDbContext db,
        IValidator<CreateCustomerRequest> validator,
        CancellationToken ct)
    {
        var validate = await validator.ValidateAsync(req, ct);

        if (!validate.IsValid)
        {
            //return validation error store in validate 
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

        var Response = Customer.Adapt<CustomerResponse>();

        // Send back 201 Created and Response with header /api/customers/{id}
        return Results.CreatedAtRoute("GetCustomerById", new { id = Customer.Id }, Response);
    }

    //Take id directly from path parameter, change to take id from current_user later after authentication is build
    public static async Task<IResult> UpdateCustomerAsync(
        long id,
        UpdateCustomerRequest req,
        [FromServices]IValidator<UpdateCustomerRequest> validator,
        CustomerDbContext db,
        CancellationToken ct)
    {

        var validate = await validator.ValidateAsync(req, ct);

        if(!validate.IsValid)
        {
            return Results.ValidationProblem(validate.ToDictionary());
        }

        var Customer = await db.Customers.Where(x => x.Id == id).FirstOrDefaultAsync(ct);

        if(Customer is null)
        {
            return Results.NotFound(new { Message = $"Customer with id {id} not found" });
        }

        Customer.Gender = req.Gender ?? Customer.Gender;
        Customer.DateOfBirth = req.DateOfBirth ?? Customer.DateOfBirth;
        Customer.Status = req.Status ?? Customer.Status;

        Customer.FullName = string.IsNullOrWhiteSpace(req.FullName) ? Customer.FullName : req.FullName.Trim();
        Customer.Phone = string.IsNullOrWhiteSpace(req.Phone) ? Customer.Phone : req.Phone.Trim();
        Customer.Email = string.IsNullOrWhiteSpace(req.Phone) ? Customer.Email : req.Phone.Trim();
        Customer.Note = string.IsNullOrWhiteSpace(req.Note) ? Customer.Note : req.Note;

        Customer.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);

        var response = Customer.Adapt<CustomerResponse>();

        return Results.Ok(Customer);
    }
}


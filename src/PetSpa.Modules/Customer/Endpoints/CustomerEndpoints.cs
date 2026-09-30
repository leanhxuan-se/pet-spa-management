using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using PetSpa.Modules.Customer.Domain.Entities;
using PetSpa.Modules.Customer.Endpoints.Requests;
using PetSpa.Modules.Customer.Infrastructure.Persistence;

namespace PetSpa.Modules.Customer.Endpoints;

public static class CustomerEndpoints
{
    //public void MapEndpoint(this IEndpointRouteBuilder app)
    //{
    //    var group = app.MapGroup("/api/customer").WithTags("Customers"); // withtag to group these Endpoint handler in a swagger called "Customers"

    //    group.MapPost("/create", CreateCustomerAsync);
    //    group
    //}

    public static async Task<IResult> CreateCustomer(CreateCustomerRequest req, CustomerDbContext db)
    {

        return Results.Created();
    }
}

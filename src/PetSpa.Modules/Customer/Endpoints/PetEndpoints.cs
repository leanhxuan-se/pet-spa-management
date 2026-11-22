using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using PetSpa.Modules.Customer.Application.Abstractions;
using PetSpa.Modules.Customer.Endpoints.Requests;
using PetSpa.Modules.Customer.Endpoints.Responses;
using PetSpa.SharedKernel.Application.Abstractions;
using PetSpa.SharedKernel.Application.Authentication;

namespace PetSpa.Modules.Customer.Endpoints;

public static class PetEndpoints
{
    public static IEndpointRouteBuilder MapPetEndpoint(this IEndpointRouteBuilder app)
    {
        var customerPets = app.MapGroup("/api/customers/{customerId:long}/pets").WithTags("Pets")
            .RequireAuthorization(AuthorizationPolicies.AuthenticatedAccount);
        customerPets.MapGet("/", GetAllPetsAsync).WithName("GetAllPets")
            .Produces<List<PetResponse>>()
            .Produces(StatusCodes.Status404NotFound);
        customerPets.MapPost("/", CreatePetAsync).WithName("CreatePet")
            .RequireAuthorization(policy => policy.RequireRole(AccountRoles.Receptionist))
            .Produces<PetResponse>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status422UnprocessableEntity);

        var group = app.MapGroup("/api/pets").WithTags("Pets")
            .RequireAuthorization(AuthorizationPolicies.AuthenticatedAccount);
        group.MapGet("/{id:long}", GetPetByIdAsync).WithName("GetPet")
            .Produces<PetResponse>()
            .Produces(StatusCodes.Status404NotFound);
        group.MapPut("/{id:long}", UpdatePetAsync).WithName("UpdatePet")
            .RequireAuthorization(policy => policy.RequireRole(AccountRoles.Receptionist))
            .Produces<PetResponse>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status403Forbidden)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status404NotFound);
        group.MapPut("/{id:long}/special-note", UpdatePetSpecialNoteAsync).WithName("UpdatePetSpecialNote")
            .RequireAuthorization(policy => policy.RequireRole(AccountRoles.Receptionist, AccountRoles.Staff))
            .Produces<PetResponse>()
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status422UnprocessableEntity);

        return app;
    }

    private static async Task<IResult> GetPetByIdAsync(
        long id,
        ClaimsPrincipal currentUser,
        IAuthorizationService authorization,
        IPetServices petServices,
        CancellationToken ct)
    {
        var owner = currentUser.GetCustomerId();
        var canManage = (await authorization.AuthorizeAsync(currentUser, AuthorizationPolicies.CustomerManagement)).Succeeded;
        if (!canManage && owner is null)
            return Results.Forbid();

        var pet = await petServices.GetByIdAsync(id, ct);
        if (pet.Status == ResultStatus.NotFound)
            return pet.ToHttpResult();

        if (pet.Value is null || (!canManage && owner != pet.Value.CustomersId))
            return Results.NotFound();

        return pet.ToHttpResult();
    }

    private static async Task<IResult> GetAllPetsAsync(
        long customerId,
        ClaimsPrincipal currentUser,
        IAuthorizationService authorization,
        IPetServices petServices,
        CancellationToken ct)
    {
        if (currentUser.GetCustomerId() != customerId &&
            !(await authorization.AuthorizeAsync(currentUser, AuthorizationPolicies.CustomerManagement)).Succeeded)
        {
            return Results.Forbid();
        }

        return (await petServices.GetAllByIdAsync(customerId, ct)).ToHttpResult();
    }

    private static async Task<IResult> CreatePetAsync(
        long customerId,
        CreatePetRequest req,
        IPetServices petServices,
        CancellationToken ct)
    {
        if (req.CustomersId != customerId)
            return Results.BadRequest(new { Message = "ID khách hàng trong URL và body phải giống nhau" });

        return (await petServices.CreateAsync(req, ct)).ToHttpResult("GetPet", pet => new { id = pet.Id });
    }

    private static async Task<IResult> UpdatePetAsync(
        long id,
        UpdatePetRequest req,
        IPetServices petServices,
        CancellationToken ct)
    {
        var pet = await petServices.UpdateAsync(id, req, ct);
        return pet.Status == ResultStatus.ValidationFailed
            ? Results.ValidationProblem(pet.Errors!)
            : pet.ToHttpResult("GetPet", value => new { id = value.Id });
    }

    private static async Task<IResult> UpdatePetSpecialNoteAsync(
        long id,
        UpdatePetSpecialNoteRequest req,
        IPetServices petServices,
        CancellationToken ct)
        => (await petServices.UpdateAsync(id, new UpdatePetRequest { SpecialNote = req.SpecialNote }, ct)).ToHttpResult();
}

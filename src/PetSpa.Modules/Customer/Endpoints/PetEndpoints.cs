using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using PetSpa.Modules.Customer.Application.Abstractions;
using PetSpa.Modules.Customer.Endpoints.Requests;
using PetSpa.Modules.Customer.Endpoints.Responses;
using System.Security.Claims;
using PetSpa.SharedKernel.Application.Abstractions;

namespace PetSpa.Modules.Customer.Endpoints;

public static class PetEndpoints
{
    public static IEndpointRouteBuilder MapPetEndpoint(this IEndpointRouteBuilder app)
    {
        var customerPets = app.MapGroup("/api/customers/{customerId:long}/pets").WithTags("Pets").RequireAuthorization();
        customerPets.MapGet("/", GetAllPetsAsync).WithName("GetAllPets");
        customerPets.MapPost("/", CreatePetAsync).WithName("CreatePet");

        var group = app.MapGroup("/api/pets").WithTags("Pets").RequireAuthorization();
        group.MapGet("/{id:long}", GetPetByIdAsync).WithName("GetPet");
        group.MapPut("/{id:long}", UpdatePetAsync).WithName("UpdatePet");

        return app;
    }

    public static async Task<IResult> GetPetByIdAsync(
        long id,
        ClaimsPrincipal currentUser,
        IPetServices petServices,
        CancellationToken ct
    )
    {
        var pet = await petServices.GetByIdAsync(id, ct);
        if (pet.Status == ResultStatus.NotFound)
        {
            return Results.NotFound(pet.Errors);
        }
        if (!CanAccessCustomer(currentUser, pet.Value!.CustomersId))
            return Results.NotFound();
        return Results.Ok<PetResponse>(pet.Value);
    }   

    public static async Task<IResult> GetAllPetsAsync(
        long customerId,
        ClaimsPrincipal currentUSer,
        IPetServices petServices,
        CancellationToken ct
    )
    {
        //Kiểm tra quyền truy cập của người dùng hiện tại
        if (!CanAccessCustomer(currentUSer, customerId))
        {
            return Results.BadRequest(new { Message = $"Bạn không có quyền truy cập pet của user khác" });
        }

        var Pets = await petServices.GetAllByIdAsync(customerId, ct);

        if (Pets.Status == ResultStatus.NotFound)
        {
            return Results.NotFound(new { Message = $"User hiện tại không có pet nào" });
        }

        return Results.Ok<List<PetResponse>>(Pets.Value);
    }

    public static async Task<IResult> CreatePetAsync(
        long customerId,
        CreatePetRequest req,
        IPetServices petServices,
        ClaimsPrincipal currentUser,
        CancellationToken ct)
    {
        //Kiểm tra quyền truy cập của người dùng hiện tại
        if (!CanAccessCustomer(currentUser, customerId))
        {
            return Results.BadRequest(new { Message = $"Bạn không có quyền tạo pet cho user khác" });
        }

        if (req.CustomersId != customerId)
            return Results.BadRequest(new { Message = "ID khách hàng trong URL và body phải giống nhau" });

        var pet = await petServices.CreateAsync(req, ct);


        return pet.ToHttpResult(
            "GetPet",
            p => new { id = p?.Id }
        );

    }

    public static async Task<IResult> UpdatePetAsync(
        long id,
        UpdatePetRequest req,
        IPetServices petServices,
        ClaimsPrincipal currentUser,
        CancellationToken ct)
    {
        var existing = await petServices.GetByIdAsync(id, ct);
        if (existing.Status == ResultStatus.NotFound || existing.Value is null || !CanAccessCustomer(currentUser, existing.Value.CustomersId))
            return Results.NotFound();
        var pet = await petServices.UpdateAsync(id, req, ct);

        switch (pet.Status)
        {
            case ResultStatus.Success:
                return Results.CreatedAtRoute("GetPet", new { id = pet?.Value?.Id }, pet?.Value);
            case ResultStatus.ValidationFailed:
                return Results.ValidationProblem(pet?.Errors);
            case ResultStatus.NotFound:
                return Results.NotFound(pet.Errors);
            default:
                return Results.BadRequest(new { errors = pet.Errors });
        }

    }

    private static bool CanAccessCustomer(ClaimsPrincipal user, long customerId)
        => (long.TryParse(user.FindFirst("customer_id")?.Value, out var userId) && userId == customerId)
            || user.FindFirst("account_role")?.Value is "MANAGER" or "RECEPTIONIST";
}


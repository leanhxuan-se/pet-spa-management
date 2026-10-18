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
        var group = app.MapGroup("/api/pet").WithTags("Pets"); // gom nhóm các endpoint liên quan đến pet lại ở swagger
            
        group.MapGet("/all_pets/{id:long}", GetAllPetsAsync).WithName("GetAllPets").RequireAuthorization();
        group.MapGet("/{customerId:long}/{petId:long}", GetPetByIdAsync).WithName("GetPet").RequireAuthorization();
        group.MapPost("/create/{id:long}", CreatePetAsync).WithName("CreatePet").RequireAuthorization();
        group.MapPut("/update/{customerId:long}/{petId:long}", UpdatePetAsync).WithName("UpdatePet").RequireAuthorization();

        return app;
    }

    public static async Task<IResult> GetPetByIdAsync(
        long customerId,
        long petId,
        ClaimsPrincipal currentUser,
        IPetServices petServices,
        CancellationToken ct
    )
    {
        //Kiểm tra quyền truy cập của người dùng hiện tại
        var currentUserId = Convert.ToInt64(currentUser.FindFirst("customer_id")?.Value);
        var currentUserRole = currentUser.FindFirst("role")?.Value;

        if (!(currentUserId == customerId || currentUserRole == "MANAGER" || currentUserRole == "RECEPTIONIST"))
        {
            return Results.BadRequest(new { Message = $"Bạn không có quyền truy cập pet của user khác" });
        }

        var pet = await petServices.GetByIdAsync(petId, ct);
        if (pet.Status == ResultStatus.NotFound)
        {
            return Results.NotFound(pet.Errors);
        }
        return Results.Ok<PetResponse>(pet.Value);
    }   

    public static async Task<IResult> GetAllPetsAsync(
        long id,
        ClaimsPrincipal currentUSer,
        IPetServices petServices,
        CancellationToken ct
    )
    {
        //Kiểm tra quyền truy cập của người dùng hiện tại
        var currentUserId = Convert.ToInt64(currentUSer.FindFirst("customer_id")?.Value);
        var currentUserRole = currentUSer.FindFirst("role")?.Value;

        if (!(currentUserId == id || currentUserRole == "MANAGER" || currentUserRole == "RECEPTIONIST"))
        {
            return Results.BadRequest(new { Message = $"Bạn không có quyền truy cập pet của user khác" });
        }

        var Pets = await petServices.GetAllByIdAsync(currentUserId, ct);

        if (Pets.Status == ResultStatus.NotFound)
        {
            return Results.NotFound(new { Message = $"User hiện tại không có pet nào" });
        }

        return Results.Ok<List<PetResponse>>(Pets.Value);
    }

    public static async Task<IResult> CreatePetAsync(
        long id,
        CreatePetRequest req,
        IPetServices petServices,
        ClaimsPrincipal currentUser,
        CancellationToken ct)
    {
        //Kiểm tra quyền truy cập của người dùng hiện tại
        var currentUserId = Convert.ToInt64(currentUser.FindFirst("customer_id")?.Value);
        var currentUserRole = currentUser.FindFirst("role")?.Value;

        if (!(currentUserId == id || currentUserRole == "MANAGER" || currentUserRole == "RECEPTIONIST"))
        {
            return Results.BadRequest(new { Message = $"Bạn không có quyền tạo pet cho user khác" });
        }

        var pet = await petServices.CreateAsync(req, ct);


        return pet.ToHttpResult(
            "GetPet",
            p => new { CustomerId = p?.CustomersId, PetId = p?.Id }
        );

    }

    public static async Task<IResult> UpdatePetAsync(
        long petId,
        long customerId,
        UpdatePetRequest req,
        IPetServices petServices,
        ClaimsPrincipal currentUser,
        CancellationToken ct)
    {
        //Kiểm tra quyền truy cập của người dùng hiện tại
        var currentUserId = Convert.ToInt64(currentUser.FindFirst("customer_id")?.Value);
        var currentUserRole = currentUser.FindFirst("role")?.Value;

        if (!(currentUserId == customerId || currentUserRole == "MANAGER" || currentUserRole == "RECEPTIONIST"))
        {
            return Results.BadRequest(new { Message = $"Bạn không có quyền cập nhật pet của user khác" });
        }
        var pet = await petServices.UpdateAsync(petId, req, ct);

        switch (pet.Status)
        {
            case ResultStatus.Success:
                return Results.CreatedAtRoute("GetPet", new { CustomerId = pet?.Value?.CustomersId, PetId = pet?.Value?.Id }, pet?.Value);
            case ResultStatus.ValidationFailed:
                return Results.ValidationProblem(pet?.Errors);
            case ResultStatus.NotFound:
                return Results.NotFound(pet.Errors);
            default:
                return Results.BadRequest(new { errors = pet.Errors });
        }

    }
}


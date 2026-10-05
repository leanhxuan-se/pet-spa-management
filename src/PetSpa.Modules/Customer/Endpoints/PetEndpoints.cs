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
            
        group.MapGet("/my_pets/{id:long}", GetPetByIdAsync).WithName("GetPetById").RequireAuthorization();
        group.MapGet("/my_pets", GetAllPetsAsync).WithName("GetMyPets").RequireAuthorization();
        group.MapPost("/create", CreatePetAsync).WithName("CreatePet").RequireAuthorization();
        group.MapPut("/update/{id:long}", UpdatePetAsync).WithName("UpdatePet").RequireAuthorization();

        return app;
    }

    public static async Task<IResult> GetPetByIdAsync(
        long id,
        long petId,
        ClaimsPrincipal currentUser,
        IPetServices petServices,
        CancellationToken ct
    )
    {
        //Kiểm tra quyền truy cập của người dùng hiện tại
        var currentUserId = Convert.ToInt64(currentUser.FindFirst("customer_id")?.Value);
        var currentUserRole = currentUser.FindFirst("role")?.Value;

        if (!(currentUserId == id || currentUserRole == "MANAGER" || currentUserRole == "RECEPTIONIST"))
        {
            return Results.BadRequest(new { Message = $"Bạn không có quyền truy cập pet của user khác" });
        }

        var pet = await petServices.GetByIdAsync(currentUserId, petId, ct);
        if (pet.Status == ResultStatus.NotFound)
        {
            return Results.NotFound(new { Message = $"Pet với id {petId} không tồn tại" });
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


        switch (pet.Status)
        {
            case ResultStatus.Success:
                return Results.CreatedAtRoute("GetPetById", new { id = pet?.Value?.Id }, pet?.Value);
            case ResultStatus.ValidationFailed:
                return Results.ValidationProblem(pet?.Errors);
            default:
                return Results.BadRequest(new { Message = "Không thể tạo khách hàng" });
        }

    }

    public static async Task<IResult> UpdatePetAsync(
        long id,
        UpdatePetRequest req,
        IPetServices petServices,
        ClaimsPrincipal currentUser,
        CancellationToken ct)
    {
        //Kiểm tra quyền truy cập của người dùng hiện tại
        var currentUserId = Convert.ToInt64(currentUser.FindFirst("customer_id")?.Value);
        var currentUserRole = currentUser.FindFirst("role")?.Value;

        if (!(currentUserId == id || currentUserRole == "MANAGER" || currentUserRole == "RECEPTIONIST"))
        {
            return Results.BadRequest(new { Message = $"Bạn không có quyền cập nhật pet của user khác" });
        }
        var pet = await petServices.UpdateAsync(id, req, ct);

        switch (pet.Status)
        {
            case ResultStatus.Success:
                return Results.CreatedAtRoute("GetPetById", new { id = pet?.Value?.Id }, pet?.Value);
            case ResultStatus.ValidationFailed:
                return Results.ValidationProblem(pet?.Errors);
            default:
                return Results.BadRequest(new { errors = pet.Errors });
        }

    }
}


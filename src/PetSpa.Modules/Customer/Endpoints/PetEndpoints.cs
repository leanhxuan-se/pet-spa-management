// Định nghĩa hoặc hỗ trợ các endpoint PetEndpoints.
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

public static class PetEndpoints
{
    // Đăng ký endpoint thú cưng.
    public static IEndpointRouteBuilder MapPetEndpoint(this IEndpointRouteBuilder app)
    {
        var customerPets = app.MapGroup("/api/customers/{customerId:long}/pets").WithTags("Pets")
            .RequireAuthorization(AuthorizationPolicies.AuthenticatedAccount);
        // 1. Đăng ký các đường dẫn và phương thức HTTP.
        customerPets.MapGet("/", GetAllPetsAsync).WithName("GetAllPets");
        customerPets.MapPost("/", CreatePetAsync).WithName("CreatePet");

        // 2. Tạo nhóm endpoint và cấu hình quyền truy cập.
        var group = app.MapGroup("/api/pets").WithTags("Pets")
            .RequireAuthorization(AuthorizationPolicies.AuthenticatedAccount);
        group.MapGet("/{id:long}", GetPetByIdAsync).WithName("GetPet");
        group.MapPut("/{id:long}", UpdatePetAsync).WithName("UpdatePet");

        return app;
    }

    // Lấy thông tin thú cưng theo mã.
    public static async Task<IResult> GetPetByIdAsync(
        long id,
        ClaimsPrincipal currentUser,
        IPetServices petServices,
        CancellationToken ct
    )
    {
        // 1. Lấy thông tin thú cưng.
        var pet = await petServices.GetByIdAsync(id, ct);
        if (pet.Status == ResultStatus.NotFound)
        {
            return Results.NotFound(pet.Errors);
        }
        // 2. Kiểm tra quyền truy cập của tài khoản.
        if (pet.Value is null || !currentUser.CanAccessCustomer(pet.Value.CustomersId))
            return Results.NotFound();
        // 3. Trả kết quả xử lý.
        return Results.Ok<PetResponse>(pet.Value);
    }   

    // Lấy danh sách thú cưng của khách hàng.
    public static async Task<IResult> GetAllPetsAsync(
        long customerId,
        ClaimsPrincipal currentUser,
        IPetServices petServices,
        CancellationToken ct
    )
    {
        // 1. Kiểm tra quyền truy cập của tài khoản.
        if (!currentUser.CanAccessCustomer(customerId))
            return Results.Forbid();

        // 2. Lấy thông tin thú cưng.
        var Pets = await petServices.GetAllByIdAsync(customerId, ct);

        if (Pets.Status == ResultStatus.NotFound)
        {
            return Results.NotFound(new { Message = $"User hiện tại không có pet nào" });
        }

        // 3. Trả kết quả xử lý.
        return Results.Ok<List<PetResponse>>(Pets.Value);
    }

    // Tạo thú cưng mới.
    public static async Task<IResult> CreatePetAsync(
        long customerId,
        CreatePetRequest req,
        IPetServices petServices,
        ClaimsPrincipal currentUser,
        CancellationToken ct)
    {
        // 1. Kiểm tra quyền truy cập của tài khoản.
        if (!currentUser.CanAccessCustomer(customerId))
            return Results.Forbid();

        if (req.CustomersId != customerId)
            return Results.BadRequest(new { Message = "ID khách hàng trong URL và body phải giống nhau" });

        // 2. Gọi dịch vụ xử lý thông tin thú cưng.
        var pet = await petServices.CreateAsync(req, ct);


        // 3. Trả kết quả xử lý.
        return pet.ToHttpResult(
            "GetPet",
            p => new { id = p?.Id }
        );

    }

    // Cập nhật thông tin thú cưng.
    public static async Task<IResult> UpdatePetAsync(
        long id,
        UpdatePetRequest req,
        IPetServices petServices,
        ClaimsPrincipal currentUser,
        CancellationToken ct)
    {
        // 1. Lấy thông tin thú cưng.
        var existing = await petServices.GetByIdAsync(id, ct);
        // 2. Kiểm tra quyền truy cập của tài khoản.
        if (existing.Status == ResultStatus.NotFound || existing.Value is null || !currentUser.CanAccessCustomer(existing.Value.CustomersId))
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
}


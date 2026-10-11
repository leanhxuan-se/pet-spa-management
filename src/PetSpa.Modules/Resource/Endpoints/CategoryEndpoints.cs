using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using PetSpa.Modules.Resource.Application.Dtos;
using PetSpa.Modules.Resource.Application.Services;
using PetSpa.Modules.Resource.Endpoints.Requests;
using PetSpa.Modules.Resource.Endpoints.Responses;

namespace PetSpa.Modules.Resource.Endpoints;

public static class CategoryEndpoints
{
    public static IEndpointRouteBuilder MapCategoryEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/resource/categories").WithTags("Categories");
        group.MapGet("/", ListAsync).WithName("GetCategories").Produces<IReadOnlyList<CategoryResponse>>();
        group.MapGet("/{id:long}", GetByIdAsync).WithName("GetCategoryById").Produces<CategoryResponse>();
        group.MapPost("/", CreateAsync).WithName("CreateCategory")
            .Produces<CategoryResponse>(StatusCodes.Status201Created).ProducesValidationProblem().RequireStaff();
        group.MapPut("/{id:long}", UpdateAsync).WithName("UpdateCategory")
            .Produces<CategoryResponse>().ProducesValidationProblem().RequireStaff();
        group.MapDelete("/{id:long}", DeleteAsync).WithName("DeleteCategory")
            .Produces(StatusCodes.Status204NoContent).RequireStaff();
        return app;
    }

    public static async Task<IResult> ListAsync(CategoryService service, CancellationToken ct)
        => Results.Ok((await service.ListAsync(ct)).Select(ToResponse).ToList());

    public static async Task<IResult> GetByIdAsync(long id, CategoryService service, CancellationToken ct)
        => (await service.GetByIdAsync(id, ct)).ToHttpResult(dto => Results.Ok(ToResponse(dto)));

    public static async Task<IResult> CreateAsync(
        CreateCategoryRequest request, CategoryService service, CancellationToken ct)
        => (await service.CreateAsync(request.Name, request.Description, request.Status, ct))
            .ToHttpResult(dto => Results.CreatedAtRoute("GetCategoryById", new { id = dto.Id }, ToResponse(dto)));

    public static async Task<IResult> UpdateAsync(
        long id, UpdateCategoryRequest request, CategoryService service, CancellationToken ct)
        => (await service.UpdateAsync(id, request.Name, request.Description, request.Status, ct))
            .ToHttpResult(dto => Results.Ok(ToResponse(dto)));

    public static async Task<IResult> DeleteAsync(long id, CategoryService service, CancellationToken ct)
        => (await service.DeleteAsync(id, ct)).ToHttpResult(_ => Results.NoContent());

    private static CategoryResponse ToResponse(CategoryDto dto)
        => new(dto.Id, dto.Name, dto.Description, dto.Status);
}

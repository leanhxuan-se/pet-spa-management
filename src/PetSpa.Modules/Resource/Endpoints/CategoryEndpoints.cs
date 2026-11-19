// Định nghĩa hoặc hỗ trợ các endpoint CategoryEndpoints.
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
    // Đăng ký endpoint danh mục.
    public static IEndpointRouteBuilder MapCategoryEndpoints(this IEndpointRouteBuilder app)
    {
        // 1. Tạo nhóm endpoint và cấu hình quyền truy cập.
        var group = app.MapGroup("/api/categories").WithTags("Categories");
        // 2. Đăng ký các đường dẫn và phương thức HTTP.
        group.MapGet("/", ListAsync).WithName("GetCategories").Produces<IReadOnlyList<CategoryResponse>>().AllowAnonymous();
        group.MapGet("/{id:long}", GetByIdAsync).WithName("GetCategoryById").Produces<CategoryResponse>().AllowAnonymous();
        group.MapPost("/", CreateAsync).WithName("CreateCategory")
            .Produces<CategoryResponse>(StatusCodes.Status201Created).ProducesValidationProblem()
            .Produces(StatusCodes.Status409Conflict).RequireResourceManager();
        group.MapPut("/{id:long}", UpdateAsync).WithName("UpdateCategory")
            .Produces<CategoryResponse>().ProducesValidationProblem()
            .Produces(StatusCodes.Status404NotFound).Produces(StatusCodes.Status409Conflict).RequireResourceManager();
        group.MapDelete("/{id:long}", DeleteAsync).WithName("DeleteCategory")
            .Produces(StatusCodes.Status204NoContent).Produces(StatusCodes.Status404NotFound).RequireResourceManager();
        return app;
    }

    // Lấy danh sách đối tượng.
    public static async Task<IResult> ListAsync(CategoryService service, CancellationToken ct)
        => Results.Ok((await service.ListAsync(ct)).Select(ToResponse).ToList());

    // Tìm đối tượng theo mã.
    public static async Task<IResult> GetByIdAsync(long id, CategoryService service, CancellationToken ct)
        => (await service.GetByIdAsync(id, ct)).ToHttpResult(dto => Results.Ok(ToResponse(dto)));

    // Tạo đối tượng mới.
    public static async Task<IResult> CreateAsync(
        CreateCategoryRequest request, CategoryService service, CancellationToken ct)
        => (await service.CreateAsync(request.Name, request.Description, request.Status, ct))
            .ToHttpResult(dto => Results.CreatedAtRoute("GetCategoryById", new { id = dto.Id }, ToResponse(dto)));

    // Cập nhật đối tượng.
    public static async Task<IResult> UpdateAsync(
        long id, UpdateCategoryRequest request, CategoryService service, CancellationToken ct)
        => (await service.UpdateAsync(id, request.Name, request.Description, request.Status, ct))
            .ToHttpResult(dto => Results.Ok(ToResponse(dto)));

    // Xóa đối tượng.
    public static async Task<IResult> DeleteAsync(long id, CategoryService service, CancellationToken ct)
        => (await service.DeleteAsync(id, ct)).ToHttpResult(_ => Results.NoContent());

    // Chuyển đối tượng thành dữ liệu phản hồi.
    private static CategoryResponse ToResponse(CategoryDto dto)
        => new(dto.Id, dto.Name, dto.Description, dto.Status);
}

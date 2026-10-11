using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using PetSpa.Modules.Resource.Application.Dtos;
using PetSpa.Modules.Resource.Application.Services;
using PetSpa.Modules.Resource.Endpoints.Requests;
using PetSpa.Modules.Resource.Endpoints.Responses;

namespace PetSpa.Modules.Resource.Endpoints;

public static class ServiceEndpoints
{
    public static IEndpointRouteBuilder MapServiceEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/resource/services").WithTags("Services");
        group.MapGet("/", ListAsync).WithName("SearchServices").Produces<IReadOnlyList<ServiceResponse>>();
        group.MapGet("/{id:long}", GetByIdAsync).WithName("GetServiceById").Produces<ServiceResponse>();
        group.MapPost("/", CreateAsync).WithName("CreateService")
            .Produces<ServiceResponse>(StatusCodes.Status201Created).ProducesValidationProblem().RequireStaff();
        group.MapPut("/{id:long}", UpdateAsync).WithName("UpdateService")
            .Produces<ServiceResponse>().ProducesValidationProblem().RequireStaff();
        group.MapDelete("/{id:long}", DeleteAsync).WithName("DeleteService")
            .Produces(StatusCodes.Status204NoContent).RequireStaff();
        app.MapGet("/api/resource/categories/{categoryId:long}/services", ListByCategoryAsync)
            .WithTags("Services").WithName("GetServicesByCategory").Produces<IReadOnlyList<ServiceResponse>>();
        return app;
    }

    public static async Task<IResult> ListAsync(
        long? categoryId, string? keyword, ServiceCatalogService service, CancellationToken ct)
        => (await service.ListAsync(categoryId, keyword, ct))
            .ToHttpResult(dtos => Results.Ok(dtos.Select(ToResponse).ToList()));

    public static async Task<IResult> ListByCategoryAsync(
        long categoryId, string? keyword, ServiceCatalogService service, CancellationToken ct)
        => (await service.ListAsync(categoryId, keyword, ct))
            .ToHttpResult(dtos => Results.Ok(dtos.Select(ToResponse).ToList()));

    public static async Task<IResult> GetByIdAsync(long id, ServiceCatalogService service, CancellationToken ct)
        => (await service.GetByIdAsync(id, ct)).ToHttpResult(dto => Results.Ok(ToResponse(dto)));

    public static async Task<IResult> CreateAsync(
        SaveServiceRequest request, ServiceCatalogService service, CancellationToken ct)
        => (await service.CreateAsync(request.CategoryId, request.ServiceName, request.Description, request.Status, ct))
            .ToHttpResult(dto => Results.CreatedAtRoute("GetServiceById", new { id = dto.Id }, ToResponse(dto)));

    public static async Task<IResult> UpdateAsync(
        long id, SaveServiceRequest request, ServiceCatalogService service, CancellationToken ct)
        => (await service.UpdateAsync(id, request.CategoryId, request.ServiceName, request.Description, request.Status, ct))
            .ToHttpResult(dto => Results.Ok(ToResponse(dto)));

    public static async Task<IResult> DeleteAsync(long id, ServiceCatalogService service, CancellationToken ct)
        => (await service.DeleteAsync(id, ct)).ToHttpResult(_ => Results.NoContent());

    private static ServiceResponse ToResponse(ServiceDto dto)
        => new(dto.Id, dto.CategoryId, dto.ServiceName, dto.Description, dto.Status);
}

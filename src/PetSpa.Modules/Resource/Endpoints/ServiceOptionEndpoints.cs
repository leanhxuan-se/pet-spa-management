using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using PetSpa.Modules.Resource.Application.Dtos;
using PetSpa.Modules.Resource.Application.Services;
using PetSpa.Modules.Resource.Endpoints.Requests;
using PetSpa.Modules.Resource.Endpoints.Responses;

namespace PetSpa.Modules.Resource.Endpoints;

public static class ServiceOptionEndpoints
{
    public static IEndpointRouteBuilder MapServiceOptionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/resource/service-options").WithTags("Service Options");
        group.MapGet("/", ListAsync).WithName("GetServiceOptions").Produces<IReadOnlyList<ServiceOptionResponse>>();
        group.MapGet("/{id:long}", GetByIdAsync).WithName("GetServiceOptionById").Produces<ServiceOptionResponse>();
        group.MapPost("/", CreateAsync).WithName("CreateServiceOption")
            .Produces<ServiceOptionResponse>(StatusCodes.Status201Created).ProducesValidationProblem().RequireStaff();
        group.MapPut("/{id:long}", UpdateAsync).WithName("UpdateServiceOption")
            .Produces<ServiceOptionResponse>().ProducesValidationProblem().RequireStaff();
        group.MapDelete("/{id:long}", DeleteAsync).WithName("DeleteServiceOption")
            .Produces(StatusCodes.Status204NoContent).RequireStaff();
        app.MapGet("/api/resource/services/{serviceId:long}/options", ListByServiceAsync)
            .WithTags("Service Options").WithName("GetOptionsByService").Produces<IReadOnlyList<ServiceOptionResponse>>();
        return app;
    }

    public static async Task<IResult> ListAsync(long? serviceId, ServiceOptionService service, CancellationToken ct)
        => (await service.ListAsync(serviceId, ct))
            .ToHttpResult(dtos => Results.Ok(dtos.Select(ToResponse).ToList()));

    public static async Task<IResult> ListByServiceAsync(long serviceId, ServiceOptionService service, CancellationToken ct)
        => (await service.ListAsync(serviceId, ct))
            .ToHttpResult(dtos => Results.Ok(dtos.Select(ToResponse).ToList()));

    public static async Task<IResult> GetByIdAsync(long id, ServiceOptionService service, CancellationToken ct)
        => (await service.GetByIdAsync(id, ct)).ToHttpResult(dto => Results.Ok(ToResponse(dto)));

    public static async Task<IResult> CreateAsync(
        SaveServiceOptionRequest request, ServiceOptionService service, CancellationToken ct)
        => (await service.CreateAsync(request.ServiceId, request.Species, request.MinWeight, request.MaxWeight,
            request.DurationMinutes, request.UnitPrice, request.Status, ct))
            .ToHttpResult(dto => Results.CreatedAtRoute("GetServiceOptionById", new { id = dto.Id }, ToResponse(dto)));

    public static async Task<IResult> UpdateAsync(
        long id, SaveServiceOptionRequest request, ServiceOptionService service, CancellationToken ct)
        => (await service.UpdateAsync(id, request.ServiceId, request.Species, request.MinWeight, request.MaxWeight,
            request.DurationMinutes, request.UnitPrice, request.Status, ct))
            .ToHttpResult(dto => Results.Ok(ToResponse(dto)));

    public static async Task<IResult> DeleteAsync(long id, ServiceOptionService service, CancellationToken ct)
        => (await service.DeleteAsync(id, ct)).ToHttpResult(_ => Results.NoContent());

    private static ServiceOptionResponse ToResponse(ServiceOptionDto dto)
        => new(dto.Id, dto.ServiceId, dto.Species, dto.MinWeight, dto.MaxWeight,
            dto.DurationMinutes, dto.UnitPrice, dto.Status);
}

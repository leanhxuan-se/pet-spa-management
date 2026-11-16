// Định nghĩa hoặc hỗ trợ các endpoint ServiceOptionEndpoints.
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
    // Đăng ký endpoint tùy chọn dịch vụ.
    public static IEndpointRouteBuilder MapServiceOptionEndpoints(this IEndpointRouteBuilder app)
    {
        // 1. Tạo nhóm endpoint và cấu hình quyền truy cập.
        var group = app.MapGroup("/api/service-options").WithTags("Service Options");
        // 2. Đăng ký các đường dẫn và phương thức HTTP.
        group.MapGet("/", ListAsync).WithName("GetServiceOptions").Produces<IReadOnlyList<ServiceOptionResponse>>().AllowAnonymous();
        group.MapGet("/{id:long}", GetByIdAsync).WithName("GetServiceOptionById").Produces<ServiceOptionResponse>().AllowAnonymous();
        group.MapPost("/", CreateAsync).WithName("CreateServiceOption")
            .Produces<ServiceOptionResponse>(StatusCodes.Status201Created).ProducesValidationProblem().RequireStaff();
        group.MapPut("/{id:long}", UpdateAsync).WithName("UpdateServiceOption")
            .Produces<ServiceOptionResponse>().ProducesValidationProblem().RequireStaff();
        group.MapDelete("/{id:long}", DeleteAsync).WithName("DeleteServiceOption")
            .Produces(StatusCodes.Status204NoContent).RequireStaff();
        app.MapGet("/api/services/{serviceId:long}/options", ListByServiceAsync)
            .WithTags("Service Options").WithName("GetOptionsByService").Produces<IReadOnlyList<ServiceOptionResponse>>().AllowAnonymous();
        return app;
    }

    // Lấy danh sách đối tượng.
    public static async Task<IResult> ListAsync(long? serviceId, ServiceOptionService service, CancellationToken ct)
        => (await service.ListAsync(serviceId, ct))
            .ToHttpResult(dtos => Results.Ok(dtos.Select(ToResponse).ToList()));

    // Lấy tùy chọn theo dịch vụ.
    public static async Task<IResult> ListByServiceAsync(long serviceId, ServiceOptionService service, CancellationToken ct)
        => (await service.ListAsync(serviceId, ct))
            .ToHttpResult(dtos => Results.Ok(dtos.Select(ToResponse).ToList()));

    // Tìm đối tượng theo mã.
    public static async Task<IResult> GetByIdAsync(long id, ServiceOptionService service, CancellationToken ct)
        => (await service.GetByIdAsync(id, ct)).ToHttpResult(dto => Results.Ok(ToResponse(dto)));

    // Tạo đối tượng mới.
    public static async Task<IResult> CreateAsync(
        SaveServiceOptionRequest request, ServiceOptionService service, CancellationToken ct)
        => (await service.CreateAsync(request.ServiceId, request.Species, request.MinWeight, request.MaxWeight,
            request.DurationMinutes, request.UnitPrice, request.Status, ct))
            .ToHttpResult(dto => Results.CreatedAtRoute("GetServiceOptionById", new { id = dto.Id }, ToResponse(dto)));

    // Cập nhật đối tượng.
    public static async Task<IResult> UpdateAsync(
        long id, SaveServiceOptionRequest request, ServiceOptionService service, CancellationToken ct)
        => (await service.UpdateAsync(id, request.ServiceId, request.Species, request.MinWeight, request.MaxWeight,
            request.DurationMinutes, request.UnitPrice, request.Status, ct))
            .ToHttpResult(dto => Results.Ok(ToResponse(dto)));

    // Xóa đối tượng.
    public static async Task<IResult> DeleteAsync(long id, ServiceOptionService service, CancellationToken ct)
        => (await service.DeleteAsync(id, ct)).ToHttpResult(_ => Results.NoContent());

    // Chuyển đối tượng thành dữ liệu phản hồi.
    private static ServiceOptionResponse ToResponse(ServiceOptionDto dto)
        => new(dto.Id, dto.ServiceId, dto.Species, dto.MinWeight, dto.MaxWeight,
            dto.DurationMinutes, dto.UnitPrice, dto.Status);
}

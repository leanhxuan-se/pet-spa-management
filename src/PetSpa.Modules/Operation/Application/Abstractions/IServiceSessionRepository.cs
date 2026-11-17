using PetSpa.Modules.Operation.Endpoints.Requests;
using PetSpa.Modules.Operation.Endpoints.Responses;
using PetSpa.Modules.Operation.Domain.Entities;

namespace PetSpa.Modules.Operation.Application.Abstractions
{
    internal interface IServiceSessionRepository
    {
        //tạo phiên dịch vụ mới dựa trên roomId, bookingDetailId
        Task<ServiceSession> CreateServiceSessionAsync(CreateServiceSessionRequest request, CancellationToken ct);
    }
}

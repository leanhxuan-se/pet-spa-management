using PetSpa.Modules.Operation.Endpoints.Requests;
using PetSpa.Modules.Operation.Endpoints.Responses;

namespace PetSpa.Modules.Operation.Application.Abstractions
{
    internal interface IStaffRepository
    {
        Task<List<StaffResponse>> GetAsync(CancellationToken ct);
        Task<StaffResponse?> GetByIdAsync(long id, CancellationToken ct);
        Task<StaffResponse> CreateAsync(CreateStaffRequest request, CancellationToken ct);
        Task<StaffResponse?> UpdateAsync(long id, UpdateStaffRequest request, CancellationToken ct);
        Task<StaffResponse?> DeleteAsync(long id, CancellationToken ct);
        Task<List<StaffResponse>> GetAvailableAtAsync(DateTime nowUtc, CancellationToken ct);
        Task<List<StaffResponse>> GetUnAvailableAtAsync(DateTime nowUtc, CancellationToken ct);
        Task<List<StaffResponse>> GetAvailableAsync(DateTime startTime, DateTime endTime, CancellationToken ct);
        Task<List<StaffResponse>> GetUnAvailableAsync(DateTime startTime, DateTime endTime, CancellationToken ct);
    }
}

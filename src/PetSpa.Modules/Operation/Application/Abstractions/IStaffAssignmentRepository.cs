using PetSpa.Modules.Operation.Endpoints.Requests;
using PetSpa.Modules.Operation.Endpoints.Responses;
using PetSpa.Modules.Operation.Domain.Entities;

namespace PetSpa.Modules.Operation.Application.Abstractions
{
    internal interface IStaffAssignmentRepository
    {
        //tạo đăng ký lịch cho nhân viên
        Task<StaffAssignment> CreateStaffAssignmentAsync(long ServiceSessionID, long StaffId, CancellationToken ct);
    }
}

using PetSpa.Modules.Operation.Application.Abstractions;
using PetSpa.Modules.Operation.Domain.Entities;
using PetSpa.Modules.Operation.Endpoints.Requests;
using PetSpa.Modules.Resource.Contracts;

namespace PetSpa.Modules.Operation.Infrastructure.Persistence.Repositories
{
    internal class StaffAssignmentRepository(OperationDbContext db) : IStaffAssignmentRepository
    {
        public async Task<StaffAssignment> CreateStaffAssignmentAsync(long serviceSessionId, long staffId, CancellationToken ct)
        {

            var staffAssignment = new StaffAssignment
            {
                ServiceSessionId = serviceSessionId,
                StaffId = staffId
            };

            await db.StaffAssignments.AddAsync(staffAssignment, ct);
            await db.SaveChangesAsync(ct);
            return staffAssignment;            
        }
    }
}

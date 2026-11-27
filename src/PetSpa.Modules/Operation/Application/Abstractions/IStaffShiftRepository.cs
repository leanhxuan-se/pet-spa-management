using PetSpa.Modules.Operation.Domain.Entities;
using PetSpa.Modules.Operation.Domain.Enums;
using PetSpa.Modules.Operation.Endpoints.Responses;

namespace PetSpa.Modules.Operation.Application.Abstractions
{
    internal sealed record StaffShiftCursor(DateOnly ShiftDate, TimeOnly StartTime, long Id);

    internal sealed record StaffShiftStaffInfo(long Id, string FullName, StaffStatus Status);

    internal interface IStaffShiftRepository
    {
        Task<IReadOnlyList<StaffShiftResponse>> GetPageAsync(
            DateOnly? date,
            long? staffId,
            StaffShiftStatus? status,
            StaffShiftCursor? after,
            int limit,
            CancellationToken ct);
        Task<List<StaffShiftResponse>> GetStaffShiftsAsync(
            DateOnly date,
            long staffId,
            CancellationToken ct);
        Task<StaffShiftStaffInfo?> GetStaffInfoAsync(long staffId, CancellationToken ct);

        Task<bool> HasOverlappingShiftAsync(
            long staffId,
            DateOnly shiftDate,
            TimeOnly startTime,
            TimeOnly endTime,
            long? excludeShiftId,
            CancellationToken ct);

        Task<StaffShift?> GetByIdAsync(long id, CancellationToken ct);

        Task<StaffShift?> GetByIdWithStaffAsync(long id, CancellationToken ct);

        Task<IReadOnlyList<long>> GetConflictingSessionIdsForShiftUpdateAsync(
            long staffId,
            DateTime oldShiftStartUtc,
            DateTime oldShiftEndUtc,
            DateTime newShiftStartUtc,
            DateTime newShiftEndUtc,
            CancellationToken ct);

        Task<bool> HasActiveAssignedSessionsInWindowAsync(
            long staffId,
            DateTime shiftStartUtc,
            DateTime shiftEndUtc,
            CancellationToken ct);

        Task<bool> HasInProgressAssignedSessionsAsync(long staffId, CancellationToken ct);

        Task AddAsync(StaffShift shift, CancellationToken ct);

        Task SaveChangesAsync(CancellationToken ct);
    }
}

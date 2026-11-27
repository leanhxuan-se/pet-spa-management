using Microsoft.EntityFrameworkCore;
using PetSpa.Modules.Operation.Application.Abstractions;
using PetSpa.Modules.Operation.Domain.Entities;
using PetSpa.Modules.Operation.Domain.Enums;
using PetSpa.Modules.Operation.Endpoints.Responses;

namespace PetSpa.Modules.Operation.Infrastructure.Persistence.Repositories
{
    internal class StaffShiftRepository(OperationDbContext db) : IStaffShiftRepository
    {
        public async Task<IReadOnlyList<StaffShiftResponse>> GetPageAsync(
            DateOnly? date,
            long? staffId,
            StaffShiftStatus? status,
            StaffShiftCursor? after,
            int limit,
            CancellationToken ct)
        {
            var query = db.StaffShifts.AsNoTracking();

            if (date.HasValue)
                query = query.Where(x => x.ShiftDate == date.Value);
            if (staffId.HasValue)
                query = query.Where(x => x.StaffId == staffId.Value);
            if (status.HasValue)
                query = query.Where(x => x.Status == status.Value);

            if (after is { } c)
            {
                query = query.Where(x =>
                    x.ShiftDate < c.ShiftDate
                    || (x.ShiftDate == c.ShiftDate && x.StartTime > c.StartTime)
                    || (x.ShiftDate == c.ShiftDate && x.StartTime == c.StartTime && x.Id > c.Id));
            }

            return await query
                .OrderByDescending(x => x.ShiftDate)
                .ThenBy(x => x.StartTime)
                .ThenBy(x => x.Id)
                .Take(limit)
                .Select(x => new StaffShiftResponse(
                    x.Id,
                    x.StaffId,
                    x.Staff.FullName,
                    x.ShiftDate,
                    x.StartTime,
                    x.EndTime,
                    x.StartedAt,
                    x.FinishedAt,
                    x.Status,
                    x.CreatedAt,
                    x.UpdatedAt))
                .ToListAsync(ct);
        }
        public async Task<List<StaffShiftResponse>> GetStaffShiftsAsync(
            DateOnly date,
            long staffId,
            CancellationToken ct)
        {
            return await db.StaffShifts
                .AsNoTracking()
                .Where(x=> x.ShiftDate >= date && x.StaffId == staffId)
                .Select(x => new StaffShiftResponse(
                    x.Id,
                    x.StaffId,
                    x.Staff.FullName,
                    x.ShiftDate,
                    x.StartTime,
                    x.EndTime,
                    x.StartedAt,
                    x.FinishedAt,
                    x.Status,
                    x.CreatedAt,
                    x.UpdatedAt))
                .ToListAsync(ct);   

                
        }

        public async Task<StaffShiftStaffInfo?> GetStaffInfoAsync(long staffId, CancellationToken ct)
        {
            return await db.Staffs
                .AsNoTracking()
                .Where(s => s.Id == staffId)
                .Select(s => new StaffShiftStaffInfo(s.Id, s.FullName, s.Status))
                .FirstOrDefaultAsync(ct);
        }

        public async Task<bool> HasOverlappingShiftAsync(
            long staffId,
            DateOnly shiftDate,
            TimeOnly startTime,
            TimeOnly endTime,
            long? excludeShiftId,
            CancellationToken ct)
        {
            return await db.StaffShifts
                .AsNoTracking()
                .AnyAsync(x =>
                    x.StaffId == staffId
                    && x.ShiftDate == shiftDate
                    && x.Status != StaffShiftStatus.CANCELLED
                    && (!excludeShiftId.HasValue || x.Id != excludeShiftId.Value)
                    && x.StartTime < endTime
                    && startTime < x.EndTime,
                    ct);
        }

        public async Task<StaffShift?> GetByIdAsync(long id, CancellationToken ct)
        {
            return await db.StaffShifts.FirstOrDefaultAsync(x => x.Id == id, ct);
        }

        public async Task<StaffShift?> GetByIdWithStaffAsync(long id, CancellationToken ct)
        {
            return await db.StaffShifts
                .Include(x => x.Staff)
                .FirstOrDefaultAsync(x => x.Id == id, ct);
        }

        public async Task<IReadOnlyList<long>> GetConflictingSessionIdsForShiftUpdateAsync(
            long staffId,
            DateTime oldShiftStartUtc,
            DateTime oldShiftEndUtc,
            DateTime newShiftStartUtc,
            DateTime newShiftEndUtc,
            CancellationToken ct)
        {
            return await db.StaffAssignments
                .AsNoTracking()
                .Where(sa =>
                    sa.StaffId == staffId
                    && sa.IsActive
                    && sa.ServiceSession.Status != ServiceSessionStatus.COMPLETED
                    && sa.ServiceSession.Status != ServiceSessionStatus.CANCELLED
                    && (
                        (sa.ServiceSession.ExpectedStartAt != null && sa.ServiceSession.ExpectedFinishAt != null
                            && sa.ServiceSession.ExpectedStartAt < oldShiftEndUtc
                            && (sa.ServiceSession.ExpectedFinishAt > oldShiftStartUtc
                                || (sa.ServiceSession.ExpectedStartAt == sa.ServiceSession.ExpectedFinishAt
                                    && sa.ServiceSession.ExpectedStartAt >= oldShiftStartUtc))
                            && (sa.ServiceSession.ExpectedStartAt < newShiftStartUtc
                                || sa.ServiceSession.ExpectedFinishAt > newShiftEndUtc))
                        || (sa.ServiceSession.ExpectedStartAt != null && sa.ServiceSession.ExpectedFinishAt == null
                            && sa.ServiceSession.ExpectedStartAt >= oldShiftStartUtc
                            && sa.ServiceSession.ExpectedStartAt < oldShiftEndUtc
                            && (sa.ServiceSession.ExpectedStartAt < newShiftStartUtc
                                || sa.ServiceSession.ExpectedStartAt >= newShiftEndUtc))
                        || (sa.ServiceSession.ExpectedStartAt == null && sa.ServiceSession.ExpectedFinishAt != null
                            && sa.ServiceSession.ExpectedFinishAt > oldShiftStartUtc
                            && sa.ServiceSession.ExpectedFinishAt <= oldShiftEndUtc
                            && (sa.ServiceSession.ExpectedFinishAt <= newShiftStartUtc
                                || sa.ServiceSession.ExpectedFinishAt > newShiftEndUtc))
                        || (sa.ServiceSession.ExpectedStartAt == null
                            && sa.ServiceSession.ExpectedFinishAt == null
                            && sa.ServiceSession.StartedAt != null
                            && sa.ServiceSession.StartedAt >= oldShiftStartUtc
                            && sa.ServiceSession.StartedAt < oldShiftEndUtc
                            && (sa.ServiceSession.StartedAt < newShiftStartUtc
                                || sa.ServiceSession.StartedAt >= newShiftEndUtc))
                    ))
                .Select(sa => sa.ServiceSessionId)
                .Distinct()
                .OrderBy(id => id)
                .ToListAsync(ct);
        }

        public async Task<bool> HasActiveAssignedSessionsInWindowAsync(
            long staffId,
            DateTime shiftStartUtc,
            DateTime shiftEndUtc,
            CancellationToken ct)
        {
            return await db.StaffAssignments
                .AsNoTracking()
                .AnyAsync(sa =>
                    sa.StaffId == staffId
                    && sa.IsActive
                    && sa.ServiceSession.Status != ServiceSessionStatus.COMPLETED
                    && sa.ServiceSession.Status != ServiceSessionStatus.CANCELLED
                    && (
                        (sa.ServiceSession.ExpectedStartAt != null && sa.ServiceSession.ExpectedFinishAt != null
                            && sa.ServiceSession.ExpectedStartAt < shiftEndUtc
                            && (sa.ServiceSession.ExpectedFinishAt > shiftStartUtc
                                || (sa.ServiceSession.ExpectedStartAt == sa.ServiceSession.ExpectedFinishAt
                                    && sa.ServiceSession.ExpectedStartAt >= shiftStartUtc)))
                        || (sa.ServiceSession.ExpectedStartAt != null && sa.ServiceSession.ExpectedFinishAt == null
                            && sa.ServiceSession.ExpectedStartAt >= shiftStartUtc
                            && sa.ServiceSession.ExpectedStartAt < shiftEndUtc)
                        || (sa.ServiceSession.ExpectedStartAt == null && sa.ServiceSession.ExpectedFinishAt != null
                            && sa.ServiceSession.ExpectedFinishAt > shiftStartUtc
                            && sa.ServiceSession.ExpectedFinishAt <= shiftEndUtc)
                        || (sa.ServiceSession.ExpectedStartAt == null
                            && sa.ServiceSession.ExpectedFinishAt == null
                            && sa.ServiceSession.StartedAt != null
                            && sa.ServiceSession.StartedAt >= shiftStartUtc
                            && sa.ServiceSession.StartedAt < shiftEndUtc)
                    ),
                    ct);
        }

        public async Task<bool> HasInProgressAssignedSessionsAsync(long staffId, CancellationToken ct)
        {
            return await db.StaffAssignments
                .AsNoTracking()
                .AnyAsync(sa =>
                    sa.StaffId == staffId
                    && sa.IsActive
                    && sa.ServiceSession.Status == ServiceSessionStatus.IN_PROGRESS,
                    ct);
        }

        public async Task AddAsync(StaffShift shift, CancellationToken ct)
        {
            await db.StaffShifts.AddAsync(shift, ct);
        }

        public async Task SaveChangesAsync(CancellationToken ct)
        {
            await db.SaveChangesAsync(ct);
        }
    }
}

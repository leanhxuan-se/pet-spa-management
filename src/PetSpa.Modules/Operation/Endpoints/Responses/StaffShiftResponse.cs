using PetSpa.Modules.Operation.Domain.Enums;

namespace PetSpa.Modules.Operation.Endpoints.Responses
{
    internal record StaffShiftResponse(
        long Id,
        long StaffId,
        string StaffName,
        DateOnly ShiftDate,
        TimeOnly StartTime,
        TimeOnly EndTime,
        DateTime? StartedAt,
        DateTime? FinishedAt,
        StaffShiftStatus Status,
        DateTime CreatedAt,
        DateTime UpdatedAt
    );
}

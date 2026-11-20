namespace PetSpa.Modules.Operation.Endpoints.Requests
{
    internal record CreateStaffShiftRequest(
        long StaffId,
        DateOnly ShiftDate,
        TimeOnly StartTime,
        TimeOnly EndTime
    );
}

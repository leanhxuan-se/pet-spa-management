namespace PetSpa.Modules.Operation.Endpoints.Requests
{
    internal record UpdateStaffShiftRequest(
        DateOnly ShiftDate,
        TimeOnly StartTime,
        TimeOnly EndTime
    );
}

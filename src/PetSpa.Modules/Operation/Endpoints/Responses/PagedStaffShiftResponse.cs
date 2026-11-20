namespace PetSpa.Modules.Operation.Endpoints.Responses
{
    internal record PagedStaffShiftResponse(
        IReadOnlyList<StaffShiftResponse> Items,
        string? NextCursor
    );
}

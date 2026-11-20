using PetSpa.Modules.Operation.Domain.Enums;

namespace PetSpa.Modules.Operation.Endpoints.Responses
{
    internal record ServiceSessionResponse(
        long Id,
        long BookingDetailId,
        long RoomId,
        string RoomName,
        ServiceSessionStatus Status,
        DateTime? ExpectedStartAt,
        DateTime? ExpectedFinishAt,
        DateTime? StartedAt,
        DateTime? FinishedAt,
        DateTime CreatedAt,
        DateTime UpdatedAt,
        IReadOnlyList<long> AssignedStaffIds
    );
}

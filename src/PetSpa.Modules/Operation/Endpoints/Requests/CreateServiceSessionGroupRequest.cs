namespace PetSpa.Modules.Operation.Endpoints.Requests;

internal sealed record CreateServiceSessionGroupRequest(long BookingId, IReadOnlyList<ServiceSessionPlan?>? Items);

internal sealed record ServiceSessionPlan(
    long BookingDetailId, long RoomId, DateTime? ExpectedStartAt, DateTime? ExpectedFinishAt);

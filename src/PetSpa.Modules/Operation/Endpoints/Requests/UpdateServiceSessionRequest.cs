namespace PetSpa.Modules.Operation.Endpoints.Requests
{
    internal record UpdateServiceSessionRequest(
        long RoomId,
        DateTime ExpectedStartAt,
        DateTime ExpectedFinishAt
    );
}

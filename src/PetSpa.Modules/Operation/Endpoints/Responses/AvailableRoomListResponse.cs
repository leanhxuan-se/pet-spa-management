
namespace PetSpa.Modules.Operation.Endpoints.Responses
{
    internal record AvailableRoomListResponse
    (
        IReadOnlyList<RoomResponse> AvailableRooms,
        IReadOnlyList<RoomResponse> UnavailableRoomNames
    );
}

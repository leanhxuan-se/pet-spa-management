
namespace PetSpa.Modules.Operation.Endpoints.Responses
{
    internal record AvailableRoomList
    (
        IReadOnlyList<RoomResponse> AvailableRooms,
        IReadOnlyList<RoomResponse> UnavailableRoomNames
    );
}

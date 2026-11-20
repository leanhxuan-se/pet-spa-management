using PetSpa.Modules.Operation.Domain.Enums;

namespace PetSpa.Modules.Operation.Endpoints.Requests
{
    internal record UpdateRoomRequest(
        string? RoomName,
        string? RoomType,
        RoomStatus? Status
        );
}

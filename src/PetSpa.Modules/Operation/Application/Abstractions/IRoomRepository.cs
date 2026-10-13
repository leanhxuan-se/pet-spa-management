

using PetSpa.Modules.Operation.Endpoints.Responses;

namespace PetSpa.Modules.Operation.Application.Abstractions
{
    internal interface IRoomRepository
    {
        Task<AvailableRoomListResponse> GetAvailableAsync(DateTime startTime, DateTime endTime);
    }
}

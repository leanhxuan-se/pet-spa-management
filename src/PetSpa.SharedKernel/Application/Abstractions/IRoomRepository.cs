

using PetSpa.Modules.Operation.Endpoints.Responses;

namespace PetSpa.Modules.Operation.Application.Abstractions
{
    internal interface IRoomRepository
    {
        Task<List<RoomResponse>> GetAvailableAsync(DateTime startTime, DateTime endTime, CancellationToken ct);
        Task<List<RoomResponse>> GetUnAvailableAsync(DateTime startTime, DateTime endTime, CancellationToken ct);
    }
}

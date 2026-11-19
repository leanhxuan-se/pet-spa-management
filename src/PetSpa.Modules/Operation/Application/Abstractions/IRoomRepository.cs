using PetSpa.Modules.Operation.Endpoints.Responses;
using PetSpa.Modules.Operation.Endpoints.Requests;

namespace PetSpa.Modules.Operation.Application.Abstractions
{
    internal interface IRoomRepository
    {

        Task<List<RoomResponse>> GetAsync(CancellationToken ct);
        Task<List<RoomResponse>> CreateAsync(CreateRoomRequest request, CancellationToken ct);
        Task<List<RoomResponse>> UpdateAsync(long id, UpdateRoomRequest request, CancellationToken ct);
        Task<List<RoomResponse>> GetAvailableAsync(DateTime startTime, DateTime endTime, CancellationToken ct);
        Task<List<RoomResponse>> GetUnAvailableAsync(DateTime startTime, DateTime endTime, CancellationToken ct);
    }
}

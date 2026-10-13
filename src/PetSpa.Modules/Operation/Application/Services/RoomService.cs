

using PetSpa.Modules.Operation.Application.Abstractions;
using PetSpa.Modules.Operation.Endpoints.Responses;
using PetSpa.SharedKernel.Application.Abstractions;

namespace PetSpa.Modules.Operation.Application.Services
{
    internal class RoomService(IRoomRepository roomRepository)
    {
        public async Task<Result<AvailableRoomListResponse>> AvailableRoom(DateTime startTime, DateTime endTime)
        {
            var response = await roomRepository.GetAvailableAsync(startTime, endTime);

            return Result<AvailableRoomListResponse>.Success(response);
        }
    }
}

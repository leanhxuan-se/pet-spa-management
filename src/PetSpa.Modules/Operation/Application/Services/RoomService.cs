

using PetSpa.Modules.Operation.Application.Abstractions;
using PetSpa.Modules.Operation.Endpoints.Responses;
using PetSpa.SharedKernel.Application.Abstractions;

namespace PetSpa.Modules.Operation.Application.Services
{
    internal class RoomService(IRoomRepository roomRepository)
    {
        public async Task<Result<AvailableRoomListResponse>> AvailableRoom(DateTime startTime, DateTime endTime, CancellationToken ct)
        {
            if(startTime > endTime)
            {
                return Result<AvailableRoomListResponse>.TimeError("Thời gian bắt đầu không được lớn hơn thời gian kết thúc!");
            }

            var response = await roomRepository.GetAvailableAsync(startTime, endTime, ct);

            return Result<AvailableRoomListResponse>.Success(response);
        }
    }
}

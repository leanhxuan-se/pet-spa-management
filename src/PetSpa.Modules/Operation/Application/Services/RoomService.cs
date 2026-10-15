

using Microsoft.AspNetCore.Http;
using PetSpa.Modules.Operation.Application.Abstractions;
using PetSpa.Modules.Operation.Endpoints.Responses;
using PetSpa.SharedKernel.Application.Abstractions;
using System.Security.Claims;

namespace PetSpa.Modules.Operation.Application.Services
{
    internal class RoomService(IRoomRepository roomRepository, ClaimsPrincipal currentUser)
    {
        public async Task<Result<AvailableRoomListResponse>> AvailableRoom(DateTime startTime, DateTime endTime, CancellationToken ct)
        {

            var currentRole = currentUser.FindFirst("account_role")?.Value;

            if (!(currentRole == "MANAGER" || currentRole == "RECEPTIONIST"))
            {

                return Result<AvailableRoomListResponse>.BadRequest("message", "khách hàng và staff Không có quyền xem thông tin phòng ");
            }

            if (startTime > endTime)
            {
                return Result<AvailableRoomListResponse>.TimeError("Thời gian bắt đầu không được lớn hơn thời gian kết thúc!");
            }

            var response = await roomRepository.GetAvailableAsync(startTime, endTime, ct);

            return Result<AvailableRoomListResponse>.Success(response);
        }
    }
}

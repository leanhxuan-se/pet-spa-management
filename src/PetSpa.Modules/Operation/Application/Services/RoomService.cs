

using Microsoft.AspNetCore.Http;
using PetSpa.Modules.Operation.Application.Abstractions;
using PetSpa.Modules.Operation.Endpoints.Responses;
using PetSpa.SharedKernel.Application.Abstractions;
using System.Security.Claims;

namespace PetSpa.Modules.Operation.Application.Services
{
    internal class RoomService(IRoomRepository roomRepository)
    {
        public async Task<Result<List<RoomResponse>>> AvailableRoom(DateTime startTime, DateTime endTime, string currentRole, CancellationToken ct)
        {


            if (!(currentRole == "MANAGER" || currentRole == "RECEPTIONIST"))
            {

                return Result<List<RoomResponse>>.BadRequest("message", "khách hàng và staff Không có quyền xem thông tin phòng ");
            }

            if (startTime >= endTime)
            {
                return Result<List<RoomResponse>>.TimeError("Thời gian bắt đầu phải nhỏ hơn thời gian kết thúc!");
            }

            var response = await roomRepository.GetAvailableAsync(startTime, endTime, ct);

            return Result<List<RoomResponse>>.Success(response);
        }
        public async Task<Result<List<RoomResponse>>> UnAvailableRoom(DateTime startTime, DateTime endTime, string currentRole, CancellationToken ct)
        {

            if (!(currentRole == "MANAGER" || currentRole == "RECEPTIONIST"))
            {

                return Result<List<RoomResponse>>.BadRequest("message", "khách hàng và staff Không có quyền xem thông tin phòng ");
            }

            if (startTime >= endTime)
            {
                return Result<List<RoomResponse>>.TimeError("Thời gian bắt đầu phải nhỏ hơn thời gian kết thúc!");
            }

            var response = await roomRepository.GetUnAvailableAsync(startTime, endTime, ct);

            return Result<List<RoomResponse>>.Success(response);
        }
    }
}

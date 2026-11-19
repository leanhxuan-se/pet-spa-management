using PetSpa.Modules.Operation.Application.Abstractions;
using PetSpa.Modules.Operation.Endpoints.Responses;
using PetSpa.Modules.Operation.Endpoints.Requests;
using PetSpa.SharedKernel.Application.Abstractions;
using System.Security.Claims;
using PetSpa.SharedKernel.Application.Authentication;

namespace PetSpa.Modules.Operation.Application.Services
{
    internal class RoomService(IRoomRepository roomRepository)
    {

        public async Task<Result<List<RoomResponse>>> GetAllRoom(ClaimsPrincipal currentUser, CancellationToken ct)
        {
            if (!currentUser.CanManageCustomers())
            {
                return Result<List<RoomResponse>>.BadRequest("message", "khách hàng và staff Không có quyền xem thông tin phòng ");
            }
            var response = await roomRepository.GetAsync(ct);
            return Result<List<RoomResponse>>.Success(response);
        }
        public async Task<Result<List<RoomResponse>>> CreateRoom(CreateRoomRequest request, ClaimsPrincipal currentUser, CancellationToken ct)
        {
            if (!currentUser.CanManageCustomers())
            {
                return Result<List<RoomResponse>>.BadRequest("message", "khách hàng và staff Không có quyền tạo phòng ");
            }
            var response = await roomRepository.CreateAsync(request, ct);
            return Result<List<RoomResponse>>.Success(response);
        }
        public async Task<Result<List<RoomResponse>>> UpdateRoom(long id, UpdateRoomRequest request, ClaimsPrincipal currentUser, CancellationToken ct)
        {
            if (!currentUser.CanManageCustomers())
            {
                return Result<List<RoomResponse>>.BadRequest("message", "khách hàng và staff Không có quyền cập nhật thông tin phòng ");
            }
            var response = await roomRepository.UpdateAsync(id, request, ct);
            return Result<List<RoomResponse>>.Success(response);
        }
        public async Task<Result<List<RoomResponse>>> AvailableRoom(DateTime startTime, DateTime endTime, ClaimsPrincipal currentUser, CancellationToken ct)
        {


            if (!currentUser.CanManageCustomers())
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
        public async Task<Result<List<RoomResponse>>> UnAvailableRoom(DateTime startTime, DateTime endTime, ClaimsPrincipal currentUser, CancellationToken ct)
        {

            if (!currentUser.CanManageCustomers())
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

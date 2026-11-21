using System.Security.Claims;
using PetSpa.Modules.Operation.Application.Abstractions;
using PetSpa.Modules.Operation.Domain.Enums;
using PetSpa.Modules.Operation.Endpoints.Requests;
using PetSpa.Modules.Operation.Endpoints.Responses;
using PetSpa.SharedKernel.Application.Abstractions;
using PetSpa.SharedKernel.Application.Authentication;

namespace PetSpa.Modules.Operation.Application.Services
{
    internal class RoomService(IRoomRepository roomRepository)
    {
        public async Task<Result<List<RoomResponse>>> GetAllRoom(ClaimsPrincipal currentUser, CancellationToken ct)
        {
            if (!currentUser.CanManageOperations())
                return Result<List<RoomResponse>>.Forbidden("Chỉ lễ tân và quản lý mới có quyền xem thông tin phòng.");

            var response = await roomRepository.GetAsync(ct);
            return Result<List<RoomResponse>>.Success(response);
        }

        public async Task<Result<List<RoomResponse>>> CreateRoom(CreateRoomRequest request, ClaimsPrincipal currentUser, CancellationToken ct)
        {
            if (!currentUser.CanManageOperations())
                return Result<List<RoomResponse>>.Forbidden("Chỉ lễ tân và quản lý mới có quyền tạo phòng.");

            var response = await roomRepository.CreateAsync(request, ct);
            return Result<List<RoomResponse>>.Success(response);
        }

        public async Task<Result<List<RoomResponse>>> UpdateRoom(long id, UpdateRoomRequest request, ClaimsPrincipal currentUser, CancellationToken ct)
        {
            if (!currentUser.CanManageOperations())
                return Result<List<RoomResponse>>.Forbidden("Chỉ lễ tân và quản lý mới có quyền cập nhật thông tin phòng.");

            if (id <= 0)
                return Result<List<RoomResponse>>.BadRequest("id", "Mã phòng phải lớn hơn 0.");

            if (request.Status is { } status && !Enum.IsDefined(status))
                return Result<List<RoomResponse>>.BadRequest("status", "Trạng thái phòng không hợp lệ.");

            return await roomRepository.UpdateAsync(id, request, ct);
        }


        // OPS-04: Tra phòng trống tại thời điểm hiện tại hoặc theo khoảng thời gian.
        public Task<Result<List<RoomResponse>>> GetAvailableRoomsAsync(
            DateTime? startTime,
            DateTime? endTime,
            ClaimsPrincipal currentUser,
            CancellationToken ct)
            => QueryRoomsByTimeAsync(startTime, endTime, available: true, currentUser, ct);

        // OPS-04: Tra phòng không khả dụng tại thời điểm hiện tại hoặc theo khoảng thời gian.
        public Task<Result<List<RoomResponse>>> GetUnavailableRoomsAsync(
            DateTime? startTime,
            DateTime? endTime,
            ClaimsPrincipal currentUser,
            CancellationToken ct)
            => QueryRoomsByTimeAsync(startTime, endTime, available: false, currentUser, ct);

        private async Task<Result<List<RoomResponse>>> QueryRoomsByTimeAsync(
            DateTime? startTime,
            DateTime? endTime,
            bool available,
            ClaimsPrincipal currentUser,
            CancellationToken ct)
        {
            if (!currentUser.CanManageOperations())
                return Result<List<RoomResponse>>.Forbidden("Chỉ lễ tân và quản lý mới có quyền tra cứu phòng.");

            if (startTime is null && endTime is null)
            {
                var nowUtc = DateTime.UtcNow;
                var currentRooms = available
                    ? await roomRepository.GetAvailableAtAsync(nowUtc, ct)
                    : await roomRepository.GetUnAvailableAtAsync(nowUtc, ct);
                return Result<List<RoomResponse>>.Success(currentRooms);
            }

            if (startTime is null || endTime is null)
                return Result<List<RoomResponse>>.BadRequest("time", "Cần truyền đủ cả startTime và endTime, hoặc bỏ trống cả hai để tra cứu hiện tại.");

            var startUtc = NormalizeUtc(startTime.Value);
            var endUtc = NormalizeUtc(endTime.Value);
            if (startUtc >= endUtc)
                return Result<List<RoomResponse>>.BadRequest("time", "Thời gian bắt đầu phải nhỏ hơn thời gian kết thúc.");

            var rangeRooms = available
                ? await roomRepository.GetAvailableAsync(startUtc, endUtc, ct)
                : await roomRepository.GetUnAvailableAsync(startUtc, endUtc, ct);

            return Result<List<RoomResponse>>.Success(rangeRooms);
        }

        private static DateTime NormalizeUtc(DateTime value) => value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };
    }
}

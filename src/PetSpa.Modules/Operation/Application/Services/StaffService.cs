using System.Security.Claims;
using PetSpa.Modules.Operation.Application.Abstractions;
using PetSpa.Modules.Operation.Endpoints.Requests;
using PetSpa.Modules.Operation.Endpoints.Responses;
using PetSpa.SharedKernel.Application.Abstractions;
using PetSpa.SharedKernel.Application.Authentication;

namespace PetSpa.Modules.Operation.Application.Services
{
    internal class StaffService(IStaffRepository _staffRepository)
    {
        public async Task<Result<List<StaffResponse>>> GetStaffsAsync(ClaimsPrincipal currentUser, CancellationToken ct)
        {
            if (!currentUser.CanManageStaff())
                return Result<List<StaffResponse>>.BadRequest("message", "khách hàng, staff và receptionist không có quyền xem thông tin phòng ");

            var staffs = await _staffRepository.GetAsync(ct);
            if (staffs is [] or null)
                return Result<List<StaffResponse>>.NotFound("Không có nhân viên nào trong hệ thống!");

            return Result<List<StaffResponse>>.Success(staffs);
        }

        public async Task<Result<StaffResponse>> GetStaffByIdAsync(long id, ClaimsPrincipal currentUser, CancellationToken ct)
        {
            if (!currentUser.CanManageStaff())
                return Result<StaffResponse>.BadRequest("message", "khách hàng, staff và receptionist không có quyền xem thông tin phòng ");

            var staff = await _staffRepository.GetByIdAsync(id, ct);
            if (staff is null)
                return Result<StaffResponse>.NotFound("Không tìm thấy nhân viên với ID đã cho!");

            return Result<StaffResponse>.Success(staff);
        }

        public async Task<Result<StaffResponse>> CreateStaffAsync(CreateStaffRequest request, ClaimsPrincipal currentUser, CancellationToken ct)
        {
            if (!currentUser.CanManageStaff())
                return Result<StaffResponse>.BadRequest("message", "khách hàng, staff và receptionist không có quyền tạo nhân viên ");

            var staff = await _staffRepository.CreateAsync(request, ct);
            return Result<StaffResponse>.Created(staff);
        }

        public async Task<Result<StaffResponse>> UpdateStaffAsync(long id, UpdateStaffRequest request, ClaimsPrincipal currentUser, CancellationToken ct)
        {
            if (!currentUser.CanManageStaff())
                return Result<StaffResponse>.BadRequest("message", "khách hàng, staff và receptionist không có quyền cập nhật thông tin nhân viên ");

            var staff = await _staffRepository.UpdateAsync(id, request, ct);
            if (staff is null)
                return Result<StaffResponse>.NotFound("Không tìm thấy nhân viên với ID đã cho!");

            return Result<StaffResponse>.Success(staff);
        }

        public async Task<Result<StaffResponse>> DeleteStaffAsync(long id, ClaimsPrincipal currentUser, CancellationToken ct)
        {
            if (!currentUser.CanManageStaff())
                return Result<StaffResponse>.BadRequest("message", "khách hàng, staff và receptionist không có quyền xóa nhân viên ");

            var staff = await _staffRepository.DeleteAsync(id, ct);
            if (staff is null)
                return Result<StaffResponse>.NotFound("Không tìm thấy nhân viên với ID đã cho!");

            return Result<StaffResponse>.Success(staff);
        }

        // OPS-05: Tra nhân viên rảnh hiện tại hoặc theo khoảng thời gian (luôn trả 200 kể cả danh sách rỗng).
        public Task<Result<List<StaffResponse>>> GetAvailableStaffsByQueryAsync(
            DateTime? startTime,
            DateTime? endTime,
            ClaimsPrincipal currentUser,
            CancellationToken ct)
            => QueryStaffsByTimeAsync(startTime, endTime, available: true, currentUser, ct);

        // OPS-05: Tra nhân viên bận hiện tại hoặc theo khoảng thời gian (luôn trả 200 kể cả danh sách rỗng).
        public Task<Result<List<StaffResponse>>> GetUnavailableStaffsByQueryAsync(
            DateTime? startTime,
            DateTime? endTime,
            ClaimsPrincipal currentUser,
            CancellationToken ct)
            => QueryStaffsByTimeAsync(startTime, endTime, available: false, currentUser, ct);

        private async Task<Result<List<StaffResponse>>> QueryStaffsByTimeAsync(
            DateTime? startTime,
            DateTime? endTime,
            bool available,
            ClaimsPrincipal currentUser,
            CancellationToken ct)
        {
            if (!currentUser.CanManageOperations())
                return Result<List<StaffResponse>>.Forbidden("Chỉ lễ tân và quản lý mới có quyền tra cứu nhân viên.");

            if (startTime is null && endTime is null)
            {
                var nowUtc = DateTime.UtcNow;
                var currentStaff = available
                    ? await _staffRepository.GetAvailableAtAsync(nowUtc, ct)
                    : await _staffRepository.GetUnAvailableAtAsync(nowUtc, ct);
                return Result<List<StaffResponse>>.Success(currentStaff);
            }

            if (startTime is null || endTime is null)
                return Result<List<StaffResponse>>.BadRequest("time", "Cần truyền đủ cả startTime và endTime, hoặc bỏ trống cả hai để tra cứu hiện tại.");

            var startUtc = NormalizeUtc(startTime.Value);
            var endUtc = NormalizeUtc(endTime.Value);
            if (startUtc >= endUtc)
                return Result<List<StaffResponse>>.BadRequest("time", "Thời gian bắt đầu phải nhỏ hơn thời gian kết thúc.");

            var rangeStaff = available
                ? await _staffRepository.GetAvailableAsync(startUtc, endUtc, ct)
                : await _staffRepository.GetUnAvailableAsync(startUtc, endUtc, ct);

            return Result<List<StaffResponse>>.Success(rangeStaff);
        }

        private static DateTime NormalizeUtc(DateTime value) => value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };
    }
}

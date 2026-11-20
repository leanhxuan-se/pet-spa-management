using PetSpa.Modules.Operation.Application.Abstractions;
using PetSpa.Modules.Operation.Endpoints.Responses;
using PetSpa.SharedKernel.Application.Abstractions;
using System.Security.Claims;
using PetSpa.SharedKernel.Application.Authentication;
using PetSpa.Modules.Operation.Endpoints.Requests;

namespace PetSpa.Modules.Operation.Application.Services
{
    internal class StaffService(IStaffRepository _staffRepository)
    {
        public async Task<Result<List<StaffResponse>>> GetStaffsAsync(ClaimsPrincipal currentUser, CancellationToken ct)
        {
            if (!currentUser.CanManageStaff())
            {
                return Result<List<StaffResponse>>.BadRequest("message", "khách hàng, staff và receptionist không có quyền xem thông tin phòng ");
            }
            var staffs = await _staffRepository.GetAsync(ct);
            if (staffs is [] or null)
            {
                return Result<List<StaffResponse>>.NotFound("Không có nhân viên nào trong hệ thống!");
            }
            return Result<List<StaffResponse>>.Success(staffs);
        }
        public async Task<Result<StaffResponse>> GetStaffByIdAsync(long id, ClaimsPrincipal currentUser, CancellationToken ct)
        {
            if (!currentUser.CanManageStaff())
            {
                return Result<StaffResponse>.BadRequest("message", "khách hàng, staff và receptionist không có quyền xem thông tin phòng ");
            }
            var staff = await _staffRepository.GetByIdAsync(id, ct);
            if (staff is null)
            {
                return Result<StaffResponse>.NotFound("Không tìm thấy nhân viên với ID đã cho!");
            }
            return Result<StaffResponse>.Success(staff);
        }
        public async Task<Result<StaffResponse>> CreateStaffAsync(CreateStaffRequest request, ClaimsPrincipal currentUser, CancellationToken ct)
        {
            if (!currentUser.CanManageStaff())
            {
                return Result<StaffResponse>.BadRequest("message", "khách hàng, staff và receptionist không có quyền tạo nhân viên ");
            }
            var staff = await _staffRepository.CreateAsync(request, ct);
            return Result<StaffResponse>.Created(staff);
        }
        public async Task<Result<StaffResponse>> UpdateStaffAsync(long id, UpdateStaffRequest request, ClaimsPrincipal currentUser, CancellationToken ct)
        {
            if (!currentUser.CanManageStaff())
            {
                return Result<StaffResponse>.BadRequest("message", "khách hàng, staff và receptionist không có quyền cập nhật thông tin nhân viên ");
            }
            var staff = await _staffRepository.UpdateAsync(id, request, ct);
            if (staff is null)
            {
                return Result<StaffResponse>.NotFound("Không tìm thấy nhân viên với ID đã cho!");
            }
            return Result<StaffResponse>.Success(staff);
        }
        public async Task<Result<StaffResponse>> DeleteStaffAsync(long id, ClaimsPrincipal currentUser, CancellationToken ct)
        {
            if (!currentUser.CanManageStaff())
            {
                return Result<StaffResponse>.BadRequest("message", "khách hàng, staff và receptionist không có quyền xóa nhân viên ");
            }
            var staff = await _staffRepository.DeleteAsync(id, ct);
            if (staff is null)
            {
                return Result<StaffResponse>.NotFound("Không tìm thấy nhân viên với ID đã cho!");
            }
            return Result<StaffResponse>.Success(staff);
        }
        public async Task<Result<List<StaffResponse>>> GetAvailableStaffsAsync(DateTime startTime, DateTime endTime, ClaimsPrincipal currentUser, CancellationToken ct)
        {

            if (!currentUser.CanManageStaff())
            {

                return Result<List<StaffResponse>>.BadRequest("message", "khách hàng, staff và receptionist không có quyền xem thông tin phòng ");
            }

            if (startTime >= endTime)
            {
                return Result<List<StaffResponse>>.TimeError("Thời gian bắt đầu phải nhỏ hơn thời gian kết thúc!");

            }

            var availStaff = await  _staffRepository.GetAvailableAsync(startTime, endTime, ct);

            if(availStaff is [] or null)
            {
                return Result<List<StaffResponse>>.NotFound("Không có nhân viên nào đang rảnh ở khoảng thời gian đã chọn!");

            }

            return Result<List<StaffResponse>>.Success(availStaff);
        }
        public async Task<Result<List<StaffResponse>>> GetUnAvailableStaffsAsync(DateTime startTime, DateTime endTime, ClaimsPrincipal currentUser, CancellationToken ct)
        {


            if (!currentUser.CanManageStaff())
            {

                return Result<List<StaffResponse>>.BadRequest("message", "khách hàng, staff và receptionist không có quyền xem thông tin phòng");
            }
            if (startTime >= endTime)
            {
                return Result<List<StaffResponse>>.TimeError("Thời gian bắt đầu phải nhỏ hơn thời gian kết thúc!");

            }

            var unAvailStaff = await _staffRepository.GetUnAvailableAsync(startTime, endTime, ct);

            if(unAvailStaff is [] or null)
            {
                return Result<List<StaffResponse>>.NotFound("Không có nhân viên nào đang bận ở khoảng thời gian đã chọn!");
            }

            return Result<List<StaffResponse>>.Success(unAvailStaff);
        }
    }
}

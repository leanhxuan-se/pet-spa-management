using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PetSpa.Modules.Operation.Domain.Enums;
using PetSpa.Modules.Operation.Infrastructure.Persistence;
using PetSpa.Modules.Operation.Application.Abstractions;
using PetSpa.Modules.Operation.Endpoints.Responses;
using PetSpa.SharedKernel.Application.Abstractions;
using System.Security.Claims;
using PetSpa.SharedKernel.Application.Authentication;
using PetSpa.Modules.Operation.Endpoints.Requests;

namespace PetSpa.Modules.Operation.Application.Services
{
    internal class StaffService(IStaffRepository _staffRepository, OperationDbContext db, IServiceProvider services)
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
        public async Task<Result<StaffResponse>> CreateStaffAsync(CreateStaffRequest request, ClaimsPrincipal actor, CancellationToken ct)
        {
            if (!Enum.IsDefined(request.Role) || !Enum.IsDefined(request.Status) ||
                !Enum.IsDefined(request.Gender) || string.IsNullOrWhiteSpace(request.FullName) ||
                string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Position) ||
                !ValidPhone(request.Phone))
                return Result<StaffResponse>.BadRequest("staff", "Thông tin nhân viên không hợp lệ.");
            if (!CanManage(actor, request.Role))
                return Result<StaffResponse>.Forbidden("Không có quyền cấp vai trò nhân viên này.");

            return Result<StaffResponse>.Created(await _staffRepository.CreateAsync(request, ct));
        }

        public Task<Result<StaffResponse>> UpdateStaffAsync(long id, UpdateStaffRequest request, ClaimsPrincipal actor, CancellationToken ct)
        {
            if ((request.Role is StaffRole role && !Enum.IsDefined(role)) ||
                (request.Status is StaffStatus status && !Enum.IsDefined(status)) ||
                (request.Phone is not null && !ValidPhone(request.Phone)))
                return Task.FromResult(Result<StaffResponse>.BadRequest("staff", "Thông tin nhân viên không hợp lệ."));

            return ChangeStaffAsync(id, request.Role, request.Status, actor,
                () => _staffRepository.UpdateAsync(id, request, ct), ct);
        }

        public Task<Result<StaffResponse>> DeleteStaffAsync(long id, ClaimsPrincipal actor, CancellationToken ct) =>
            ChangeStaffAsync(id, null, StaffStatus.INACTIVE, actor, () => _staffRepository.DeleteAsync(id, ct), ct);

        private async Task<Result<StaffResponse>> ChangeStaffAsync(long id, StaffRole? role, StaffStatus? status,
            ClaimsPrincipal actor, Func<Task<StaffResponse?>> save, CancellationToken ct)
        {
            if (!actor.CanManageStaff() || (role.HasValue && !CanManage(actor, role.Value)))
                return Result<StaffResponse>.Forbidden("Không có quyền thay đổi hồ sơ nhân viên này.");

            await using var transaction = await db.Database.BeginTransactionAsync(ct);
            await db.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT pg_advisory_xact_lock({StaffAccountAdministration.AdvisoryLockKey})", ct);
            var guard = services.GetRequiredService<IStaffAccountGuard>();
            if (!await guard.CanActAsync(actor, ct))
                return Result<StaffResponse>.Forbidden("Không còn quyền thay đổi hồ sơ nhân viên.");

            var staff = await _staffRepository.GetByIdAsync(id, ct);
            if (staff is null)
                return Result<StaffResponse>.NotFound("Không tìm thấy nhân viên với ID đã cho!");
            if (!CanManage(actor, staff.Role))
                return Result<StaffResponse>.Forbidden("Không có quyền thay đổi hồ sơ nhân viên này.");

            if (staff is { Role: StaffRole.ADMIN, Status: StaffStatus.ACTIVE } &&
                (status == StaffStatus.INACTIVE || role is not (null or StaffRole.ADMIN)) &&
                !await guard.CanDeactivateAdminAsync(id, ct))
                return Result<StaffResponse>.Conflict("Cần giữ ít nhất một tài khoản admin có thể đăng nhập.");

            var updated = await save();
            await transaction.CommitAsync(ct);
            return updated is null
                ? Result<StaffResponse>.NotFound("Không tìm thấy nhân viên với ID đã cho!")
                : Result<StaffResponse>.Success(updated);
        }

        private static bool CanManage(ClaimsPrincipal actor, StaffRole targetRole) =>
            StaffAccountAdministration.CanManageRole(actor.FindFirst(AuthClaimTypes.Role)?.Value,
                targetRole.ToString().ToLowerInvariant());

        private static bool ValidPhone(string? phone) => phone is not null &&
            AccountPhone.Normalize(phone).Length is > 0 and <= 30;


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

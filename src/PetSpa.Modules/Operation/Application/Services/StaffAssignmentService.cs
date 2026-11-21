// Dịch vụ nghiệp vụ phân công nhân viên (OPS-07, OPS-07A, OPS-07B).
using System.Security.Claims;
using PetSpa.Modules.Operation.Application.Abstractions;
using PetSpa.Modules.Operation.Domain.Entities;
using PetSpa.Modules.Operation.Domain.Enums;
using PetSpa.Modules.Operation.Endpoints.Requests;
using PetSpa.Modules.Operation.Endpoints.Responses;
using PetSpa.SharedKernel.Application.Abstractions;
using PetSpa.SharedKernel.Application.Authentication;

namespace PetSpa.Modules.Operation.Application.Services
{
    // Xử lý phân công lần đầu kèm thông báo, đổi người phụ trách và hủy phân công mềm giữ lịch sử.
    internal class StaffAssignmentService(
        IStaffAssignmentRepository assignmentRepository,
        StaffNotificationService notificationService)
    {
        // OPS-07: Phân công nhân viên cho phiên WAITING và tự động tạo 1 thông báo chưa đọc trong cùng transaction.
        public async Task<Result<StaffAssignmentResponse>> AssignAsync(
            long serviceSessionId,
            AssignStaffRequest? request,
            ClaimsPrincipal currentUser,
            CancellationToken ct)
        {
            // 1. Kiểm tra quyền điều phối (Lễ tân, Quản lý, Admin).
            if (!currentUser.CanManageOperations())
                return Result<StaffAssignmentResponse>.Forbidden("Chỉ lễ tân và quản lý mới có quyền phân công nhân viên.");

            // 2. Validate dữ liệu đầu vào.
            if (serviceSessionId <= 0)
                return Result<StaffAssignmentResponse>.BadRequest("id", "Mã phiên dịch vụ phải lớn hơn 0.");

            if (request is null || request.StaffId <= 0)
                return Result<StaffAssignmentResponse>.BadRequest("staffId", "Mã nhân viên phải lớn hơn 0.");

            return await assignmentRepository.ExecuteInTransactionAsync(async token =>
            {
                // 3. Tìm phiên dịch vụ.
                var session = await assignmentRepository.GetSessionWithAssignmentsAsync(serviceSessionId, token);
                if (session is null)
                    return Result<StaffAssignmentResponse>.NotFound("Không tìm thấy phiên dịch vụ.");

                // 4. Idempotent: Nếu nhân viên này đã có phân công còn hiệu lực trên phiên -> trả 200 OK ngay, không tạo thông báo mới.
                var existingActive = session.StaffAssignments
                    .FirstOrDefault(sa => sa.IsActive && sa.StaffId == request.StaffId);
                if (existingActive is not null)
                    return Result<StaffAssignmentResponse>.Success(ToResponse(existingActive, existingActive.Staff.FullName, notificationId: null));

                // 5. Kiểm tra phiên còn ở trạng thái WAITING mới được phép phân công.
                if (session.Status != ServiceSessionStatus.WAITING)
                    return Result<StaffAssignmentResponse>.Conflict($"Chỉ có thể phân công khi phiên đang ở trạng thái WAITING (trạng thái hiện tại: {session.Status}).");

                // 6. Kiểm tra điều kiện nhân viên, ca làm bao phủ và trùng lịch với phiên khác.
                var (staff, eligibilityError) = await ValidateStaffEligibilityAsync(session, request.StaffId, token);
                if (eligibilityError is not null)
                    return eligibilityError;

                // 7. Ghi phân công mới và thông báo nội bộ lần đầu trong cùng một database transaction.

                var assignment = new StaffAssignment
                {
                    ServiceSessionId = session.Id,
                    StaffId = staff!.Id,
                    IsActive = true,
                    AssignedAt = DateTime.UtcNow
                };

                await assignmentRepository.AddAsync(assignment, token);
                await assignmentRepository.SaveChangesAsync(token);

                var notification = await notificationService.CreateAssignmentNotificationAsync(
                    session.Id,
                    assignment.Id,
                    session.Room.RoomName,
                    session.ExpectedStartAt,
                    StaffNotificationType.ASSIGNED,
                    $"assign:{assignment.Id}",
                    token);

                return Result<StaffAssignmentResponse>.Created(ToResponse(assignment, staff.FullName, notification.Id));
            }, ct);
        }

        // OPS-07A: Đổi nhân viên phụ trách phân công, lưu lịch sử thu hồi người cũ và gửi thông báo cho người mới.
        public async Task<Result<StaffAssignmentResponse>> ReassignAsync(
            long serviceSessionId,
            long assignmentId,
            ReassignStaffRequest? request,
            ClaimsPrincipal currentUser,
            CancellationToken ct)
        {
            // 1. Kiểm tra quyền điều phối (Lễ tân, Quản lý, Admin).
            if (!currentUser.CanManageOperations())
                return Result<StaffAssignmentResponse>.Forbidden("Chỉ lễ tân và quản lý mới có quyền điều chỉnh phân công.");

            // 2. Validate tham số đầu vào.
            if (serviceSessionId <= 0)
                return Result<StaffAssignmentResponse>.BadRequest("id", "Mã phiên dịch vụ phải lớn hơn 0.");

            if (assignmentId <= 0)
                return Result<StaffAssignmentResponse>.BadRequest("assignmentId", "Mã phân công phải lớn hơn 0.");

            if (request is null || request.StaffId <= 0)
                return Result<StaffAssignmentResponse>.BadRequest("staffId", "Mã nhân viên thay thế phải lớn hơn 0.");

            return await assignmentRepository.ExecuteInTransactionAsync(async token =>
            {
                // 3. Tìm phiên dịch vụ và phân công thuộc đúng phiên đó.
                var session = await assignmentRepository.GetSessionWithAssignmentsAsync(serviceSessionId, token);
                if (session is null)
                    return Result<StaffAssignmentResponse>.NotFound("Không tìm thấy phiên dịch vụ.");

                var oldAssignment = session.StaffAssignments.FirstOrDefault(sa => sa.Id == assignmentId);
                if (oldAssignment is null)
                    return Result<StaffAssignmentResponse>.NotFound("Không tìm thấy phân công thuộc phiên dịch vụ này.");

                // 4. Chỉ cho điều chỉnh phân công còn hiệu lực trên phiên đang ở trạng thái WAITING.
                if (!oldAssignment.IsActive)
                    return Result<StaffAssignmentResponse>.Conflict("Phân công này đã bị thu hồi hoặc đã được thay thế trước đó.");

                if (session.Status != ServiceSessionStatus.WAITING)
                    return Result<StaffAssignmentResponse>.Conflict($"Phiên đã qua trạng thái WAITING ({session.Status}), không thể đổi phân công.");

                // 5. Idempotent: Nếu chọn đúng nhân viên đang phụ trách -> trả 200 OK và KHÔNG tạo thông báo mới.
                if (oldAssignment.StaffId == request.StaffId)
                    return Result<StaffAssignmentResponse>.Success(ToResponse(oldAssignment, oldAssignment.Staff.FullName, notificationId: null));

                // 6. Không cho đổi sang nhân viên đang có một phân công hiệu lực khác trên cùng phiên.
                var alreadyAssignedElsewhere = session.StaffAssignments
                    .Any(sa => sa.IsActive && sa.Id != oldAssignment.Id && sa.StaffId == request.StaffId);
                if (alreadyAssignedElsewhere)
                    return Result<StaffAssignmentResponse>.Conflict("Nhân viên thay thế đã có phân công hiệu lực khác trên phiên dịch vụ này.");

                // 7. Kiểm tra người mới đủ điều kiện (ACTIVE, có ca bao phủ, không trùng phiên khác); vi phạm -> giữ nguyên phân công cũ.
                var (newStaff, eligibilityError) = await ValidateStaffEligibilityAsync(session, request.StaffId, token);
                if (eligibilityError is not null)
                    return eligibilityError;

                // 8. Trong cùng transaction: tạo phân công mới, vô hiệu hóa phân công cũ (lưu lịch sử) và gửi thông báo cho người mới.

                var now = DateTime.UtcNow;
                var newAssignment = new StaffAssignment
                {
                    ServiceSessionId = session.Id,
                    StaffId = newStaff!.Id,
                    IsActive = true,
                    AssignedAt = now
                };

                await assignmentRepository.AddAsync(newAssignment, token);
                await assignmentRepository.SaveChangesAsync(token);

                oldAssignment.IsActive = false;
                oldAssignment.RevokedAt = now;
                oldAssignment.ReplacedByAssignmentId = newAssignment.Id;
                await assignmentRepository.SaveChangesAsync(token);

                var notification = await notificationService.CreateAssignmentNotificationAsync(
                    session.Id,
                    newAssignment.Id,
                    session.Room.RoomName,
                    session.ExpectedStartAt,
                    StaffNotificationType.REASSIGNED,
                    $"reassign:{newAssignment.Id}",
                    token);

                return Result<StaffAssignmentResponse>.Success(ToResponse(newAssignment, newStaff.FullName, notification.Id));
            }, ct);
        }

        // OPS-07B: Hủy phân công mềm trên phiên WAITING (lưu lịch sử và thu hồi quyền nhân viên cũ).
        public async Task<Result<bool>> RevokeAsync(
            long serviceSessionId,
            long assignmentId,
            ClaimsPrincipal currentUser,
            CancellationToken ct)
        {
            // 1. Kiểm tra quyền điều phối (Lễ tân, Quản lý, Admin).
            if (!currentUser.CanManageOperations())
                return Result<bool>.Forbidden("Chỉ lễ tân và quản lý mới có quyền hủy phân công.");

            // 2. Validate tham số đầu vào.
            if (serviceSessionId <= 0)
                return Result<bool>.BadRequest("id", "Mã phiên dịch vụ phải lớn hơn 0.");

            if (assignmentId <= 0)
                return Result<bool>.BadRequest("assignmentId", "Mã phân công phải lớn hơn 0.");

            return await assignmentRepository.ExecuteInTransactionAsync(async token =>
            {
                // 3. Kiểm tra phiên và phân công thuộc đúng phiên đó.
                var session = await assignmentRepository.GetSessionWithAssignmentsAsync(serviceSessionId, token);
                if (session is null)
                    return Result<bool>.NotFound("Không tìm thấy phiên dịch vụ.");

                var assignment = session.StaffAssignments.FirstOrDefault(sa => sa.Id == assignmentId);
                if (assignment is null)
                    return Result<bool>.NotFound("Không tìm thấy phân công thuộc phiên dịch vụ này.");

                // 4. Idempotent: Phân công đã vô hiệu hóa trước đó -> trả thẳng 204 NoContent không ghi lại DB.
                if (!assignment.IsActive)
                    return Result<bool>.NoContent();

                // 5. Không cho hủy phân công khi phiên đã bắt đầu, hoàn tất hoặc đã hủy.
                if (session.Status != ServiceSessionStatus.WAITING)
                    return Result<bool>.Conflict("Phiên đã khóa phân công.");

                // 6. Vô hiệu hóa phân công (soft cancel) để giữ lịch sử ai từng được giao và giải phóng nhân viên.
                assignment.IsActive = false;
                assignment.RevokedAt = DateTime.UtcNow;
                await assignmentRepository.SaveChangesAsync(ct);

                return Result<bool>.NoContent();
            }, ct);
        }

        // Kiểm tra tổng hợp điều kiện nhận việc của nhân viên theo thời gian dự kiến của phiên.
        private async Task<(Staff? Staff, Result<StaffAssignmentResponse>? Error)> ValidateStaffEligibilityAsync(
            ServiceSession session,
            long staffId,
            CancellationToken ct)
        {
            if (!session.ExpectedStartAt.HasValue || !session.ExpectedFinishAt.HasValue)
            {
                return (null, Result<StaffAssignmentResponse>.Conflict(
                    "Phiên dịch vụ chưa có đầy đủ thời gian dự kiến bắt đầu và kết thúc nên chưa thể phân công."));
            }

            var startUtc = NormalizeUtc(session.ExpectedStartAt.Value);
            var endUtc = NormalizeUtc(session.ExpectedFinishAt.Value);
            if (startUtc >= endUtc)
            {
                return (null, Result<StaffAssignmentResponse>.Conflict(
                    "Khung giờ dự kiến của phiên dịch vụ không hợp lệ."));
            }

            var staff = await assignmentRepository.GetStaffByIdAsync(staffId, ct);
            if (staff is null)
            {
                return (null, Result<StaffAssignmentResponse>.Conflict(
                    "Nhân viên được chọn không tồn tại trong hệ thống."));
            }

            if (staff.Status != StaffStatus.ACTIVE)
            {
                return (null, Result<StaffAssignmentResponse>.Conflict(
                    $"Nhân viên '{staff.FullName}' hiện không ở trạng thái hoạt động ({staff.Status})."));
            }

            var startDate = DateOnly.FromDateTime(startUtc);
            var endDate = DateOnly.FromDateTime(endUtc);
            if (startDate != endDate)
            {
                return (null, Result<StaffAssignmentResponse>.Conflict(
                    "Khung giờ phiên vượt qua ranh giới ngày nên không có ca làm việc bao phủ trọn phiên."));
            }

            var hasShift = await assignmentRepository.HasCoveringShiftAsync(
                staff.Id,
                startDate,
                TimeOnly.FromDateTime(startUtc),
                TimeOnly.FromDateTime(endUtc),
                ct);
            if (!hasShift)
            {
                return (null, Result<StaffAssignmentResponse>.Conflict(
                    $"Nhân viên '{staff.FullName}' không có ca làm (SCHEDULED/WORKING) bao phủ trọn khoảng thời gian của phiên."));
            }

            var hasOverlap = await assignmentRepository.HasOverlappingActiveAssignmentAsync(
                staff.Id,
                startUtc,
                endUtc,
                session.Id,
                ct);
            if (hasOverlap)
            {
                return (null, Result<StaffAssignmentResponse>.Conflict(
                    $"Nhân viên '{staff.FullName}' đã được phân công cho phiên dịch vụ khác trùng khung giờ."));
            }

            return (staff, null);
        }

        private static StaffAssignmentResponse ToResponse(
            StaffAssignment assignment,
            string staffName,
            long? notificationId) =>
            new(
                assignment.Id,
                assignment.ServiceSessionId,
                assignment.StaffId,
                staffName,
                assignment.IsActive,
                assignment.AssignedAt,
                assignment.RevokedAt,
                assignment.ReplacedByAssignmentId,
                notificationId);

        private static DateTime NormalizeUtc(DateTime value) => value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };
    }
}

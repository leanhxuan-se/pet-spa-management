// Dịch vụ nghiệp vụ quản lý thông báo nội bộ cho nhân viên (dùng chung OPS-07, OPS-07A, OPS-07C, OPS-11).
using System.Globalization;
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
    // Xử lý tạo thông báo giao việc/đổi người/nhắc việc, tải thông báo và đánh dấu đã đọc.
    internal class StaffNotificationService(IStaffNotificationRepository notificationRepository)
    {
        private const int MaxIdempotencyKeyLength = 100;

        // Nghiệp vụ nội bộ tạo đúng 1 thông báo chưa đọc gắn khóa chống trùng; tham gia cùng transaction hiện tại nếu có.
        public async Task<StaffNotification> CreateAssignmentNotificationAsync(
            long serviceSessionId,
            long staffAssignmentId,
            string roomName,
            DateTime? expectedStartAt,
            StaffNotificationType type,
            string? idempotencyKey,
            CancellationToken ct)
        {
            var (title, message) = BuildNotificationContent(serviceSessionId, roomName, expectedStartAt, type);

            var notification = new StaffNotification
            {
                StaffAssignmentId = staffAssignmentId,
                Type = type,
                Title = title,
                Message = message,
                IsRead = false,
                ReadAt = null,
                IdempotencyKey = idempotencyKey,
                CreatedAt = DateTime.UtcNow
            };

            await notificationRepository.AddAsync(notification, ct);
            await notificationRepository.SaveChangesAsync(ct);

            return notification;
        }

        // OPS-07C: Gửi nhắc việc thủ công cho một phân công còn hiệu lực trên phiên WAITING.
        public async Task<Result<StaffNotificationResponse>> SendReminderAsync(
            long serviceSessionId,
            RemindAssignmentRequest? request,
            string? idempotencyKeyHeader,
            ClaimsPrincipal currentUser,
            CancellationToken ct)
        {
            // 1. Kiểm tra người gọi phải là tài khoản nhân viên hợp lệ trước khi tra dữ liệu.
            if (!currentUser.HasStaffRole())
                return Result<StaffNotificationResponse>.Forbidden("Bạn không có quyền gửi nhắc việc.");

            // 2. Validate tham số đầu vào và độ dài khóa chống lặp tùy chọn.
            if (serviceSessionId <= 0)
                return Result<StaffNotificationResponse>.BadRequest("id", "Mã phiên dịch vụ phải lớn hơn 0.");

            if (request is null || request.AssignmentId <= 0)
                return Result<StaffNotificationResponse>.BadRequest("assignmentId", "Mã phân công phải lớn hơn 0.");

            var trimmedKey = string.IsNullOrWhiteSpace(idempotencyKeyHeader) ? null : idempotencyKeyHeader.Trim();
            if (trimmedKey is { Length: > MaxIdempotencyKeyLength })
                return Result<StaffNotificationResponse>.BadRequest("idempotencyKey", $"Header Idempotency-Key không được vượt quá {MaxIdempotencyKeyLength} ký tự.");

            // 3. Đọc thông tin phiên dịch vụ kèm các phân công để kiểm tra quyền và người nhận.
            var session = await notificationRepository.GetSessionWithAssignmentsAsync(serviceSessionId, ct);
            if (session is null)
                return Result<StaffNotificationResponse>.NotFound("Không tìm thấy phiên dịch vụ.");

            // 4. Phân quyền: Lễ tân/Quản lý/Admin hoặc nhân viên đang có phân công hiệu lực trên phiên này.
            var callerStaffId = currentUser.GetStaffId();
            var isAssignedOnSession = callerStaffId.HasValue
                && session.StaffAssignments.Any(sa => sa.IsActive && sa.StaffId == callerStaffId.Value);

            if (!currentUser.CanManageOperations() && !isAssignedOnSession)
                return Result<StaffNotificationResponse>.Forbidden("Bạn không có quyền gửi nhắc việc cho phiên dịch vụ này.");

            // 5. Kiểm tra trạng thái phiên: chỉ cho phép nhắc việc khi phiên còn ở trạng thái WAITING.
            if (session.Status != ServiceSessionStatus.WAITING)
                return Result<StaffNotificationResponse>.Conflict($"Chỉ có thể gửi nhắc việc khi phiên đang ở trạng thái WAITING (trạng thái hiện tại: {session.Status}).");

            // 6. Tìm đúng phân công thuộc phiên và kiểm tra còn hiệu lực (server tự xác định người nhận từ assignment.StaffId).
            var assignment = session.StaffAssignments.FirstOrDefault(sa => sa.Id == request.AssignmentId);
            if (assignment is null)
                return Result<StaffNotificationResponse>.NotFound("Không tìm thấy phân công thuộc phiên dịch vụ này.");

            if (!assignment.IsActive)
                return Result<StaffNotificationResponse>.Conflict("Phân công đã bị thu hồi hoặc thay thế, không thể gửi nhắc việc.");

            // 7. Kiểm tra Idempotency-Key nếu client gửi lại cùng một lần nhấn nhắc việc.
            string? scopedKey = null;
            if (trimmedKey is not null)
            {
                scopedKey = $"remind:{serviceSessionId}:{assignment.Id}:{trimmedKey}";
                var existing = await notificationRepository.GetByIdempotencyKeyAsync(scopedKey, ct);
                if (existing is not null)
                    return Result<StaffNotificationResponse>.Success(ToResponse(existing, assignment.StaffId, session.Id));
            }

            // 8. Dùng lại nghiệp vụ tạo thông báo nội bộ để ghi một thông báo nhắc mới (chưa đọc).
            var created = await CreateAssignmentNotificationAsync(
                session.Id,
                assignment.Id,
                session.Room.RoomName,
                session.ExpectedStartAt,
                StaffNotificationType.REMINDER,
                scopedKey,
                ct);

            return Result<StaffNotificationResponse>.Created(ToResponse(created, assignment.StaffId, session.Id));
        }

        // OPS-07C & OPS-11: Lấy danh sách thông báo và số lượng chưa đọc của nhân viên đang đăng nhập.
        public async Task<Result<MyNotificationListResponse>> GetMyNotificationsAsync(
            ClaimsPrincipal currentUser,
            CancellationToken ct)
        {
            // Suy ra staff_id từ JWT, chỉ cho phép tài khoản nhân viên hợp lệ.
            if (!currentUser.HasStaffRole() || currentUser.GetStaffId() is not { } staffId)
                return Result<MyNotificationListResponse>.Forbidden("Chỉ tài khoản nhân viên mới có quyền xem thông báo.");

            var items = await notificationRepository.GetByStaffIdAsync(staffId, ct);
            var unreadCount = items.Count(n => !n.IsRead);

            return Result<MyNotificationListResponse>.Success(new MyNotificationListResponse(unreadCount, items));
        }

        // OPS-07C: Đánh dấu thông báo đã đọc cho nhân viên sở hữu thông báo.
        public async Task<Result<bool>> MarkAsReadAsync(
            long notificationId,
            ClaimsPrincipal currentUser,
            CancellationToken ct)
        {
            // 1. Kiểm tra quyền nhân viên và lấy staffId từ JWT.
            if (!currentUser.HasStaffRole() || currentUser.GetStaffId() is not { } staffId)
                return Result<bool>.Forbidden("Chỉ tài khoản nhân viên mới có quyền đánh dấu đã đọc thông báo.");

            if (notificationId <= 0)
                return Result<bool>.BadRequest("id", "Mã thông báo phải lớn hơn 0.");

            // 2. Tìm thông báo và xác minh quyền sở hữu của người nhận.
            var notification = await notificationRepository.GetByIdAsync(notificationId, ct);
            if (notification is null)
                return Result<bool>.NotFound("Không tìm thấy thông báo.");

            if (notification.StaffAssignment.StaffId != staffId)
                return Result<bool>.Forbidden("Bạn không có quyền thao tác trên thông báo của nhân viên khác.");

            // 3. Idempotent: nếu đã đọc trước đó thì giữ nguyên ReadAt và trả 204 ngay.
            if (notification.IsRead)
                return Result<bool>.NoContent();

            notification.IsRead = true;
            notification.ReadAt = DateTime.UtcNow;
            await notificationRepository.SaveChangesAsync(ct);

            return Result<bool>.NoContent();
        }

        private static (string Title, string Message) BuildNotificationContent(
            long serviceSessionId,
            string roomName,
            DateTime? expectedStartAt,
            StaffNotificationType type)
        {
            var startTimeText = expectedStartAt.HasValue
                ? expectedStartAt.Value.ToString("yyyy-MM-dd HH:mm 'UTC'", CultureInfo.InvariantCulture)
                : "chưa xác định";

            return type switch
            {
                StaffNotificationType.ASSIGNED => (
                    "Phân công phiên dịch vụ mới",
                    $"Bạn được phân công phiên dịch vụ #{serviceSessionId} tại phòng {roomName} (dự kiến: {startTimeText})."),
                StaffNotificationType.REASSIGNED => (
                    "Điều chỉnh phân công phiên dịch vụ",
                    $"Bạn vừa được giao thay thế cho phiên dịch vụ #{serviceSessionId} tại phòng {roomName} (dự kiến: {startTimeText})."),
                StaffNotificationType.REMINDER => (
                    "Nhắc việc phiên dịch vụ",
                    $"Nhắc việc: Bạn có phiên dịch vụ #{serviceSessionId} tại phòng {roomName} (dự kiến: {startTimeText})."),
                _ => throw new ArgumentOutOfRangeException(nameof(type))
            };
        }

        private static StaffNotificationResponse ToResponse(StaffNotification notification, long staffId, long serviceSessionId) =>
            new(
                notification.Id,
                staffId,
                serviceSessionId,
                notification.StaffAssignmentId,
                notification.Type,
                notification.Title,
                notification.Message,
                notification.IsRead,
                notification.ReadAt,
                notification.CreatedAt);
    }
}

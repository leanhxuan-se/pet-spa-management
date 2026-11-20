// Dịch vụ nghiệp vụ xử lý phiên dịch vụ (OPS-06A, OPS-07C chi tiết phiên, OPS-08, OPS-11).
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
    // Xử lý xem chi tiết phiên, điều chỉnh phòng/giờ dự kiến, bắt đầu thực hiện phiên và danh sách việc cá nhân.
    internal class ServiceSessionService(IServiceSessionRepository serviceSessionRepository)
    {
        // OPS-07C: Xem chi tiết phiên dịch vụ (kiểm tra lại quyền phân công còn hiệu lực đối với nhân viên).
        public async Task<Result<ServiceSessionResponse>> GetByIdAsync(
            long id,
            ClaimsPrincipal currentUser,
            CancellationToken ct)
        {
            // 1. Chỉ tài khoản nhân viên hợp lệ mới được truy cập phiên dịch vụ.
            if (!currentUser.HasStaffRole())
                return Result<ServiceSessionResponse>.Forbidden("Bạn không có quyền xem chi tiết phiên dịch vụ.");

            if (id <= 0)
                return Result<ServiceSessionResponse>.BadRequest("id", "Mã phiên dịch vụ phải lớn hơn 0.");

            // 2. Đọc chi tiết phiên dịch vụ.
            var session = await serviceSessionRepository.GetByIdReadOnlyAsync(id, ct);
            if (session is null)
                return Result<ServiceSessionResponse>.NotFound("Không tìm thấy phiên dịch vụ.");

            // 3. Nếu không phải Lễ tân/Quản lý/Admin thì phải có phân công còn hiệu lực (chưa bị thu hồi) trên phiên này.
            if (!currentUser.CanManageOperations())
            {
                var staffId = currentUser.GetStaffId();
                var hasActiveAssignment = staffId.HasValue
                    && session.StaffAssignments.Any(sa => sa.IsActive && sa.StaffId == staffId.Value);

                if (!hasActiveAssignment)
                    return Result<ServiceSessionResponse>.Forbidden("Đã thu hồi phân công hoặc bạn không có quyền truy cập phiên dịch vụ này.");
            }

            var activeStaffIds = GetActiveStaffIds(session);
            return Result<ServiceSessionResponse>.Success(ToResponse(session, session.Room?.RoomName ?? string.Empty, activeStaffIds));
        }

        // OPS-06A: Điều chỉnh phòng và khung giờ dự kiến cho phiên WAITING.
        public async Task<Result<ServiceSessionResponse>> UpdateAsync(
            long id,
            UpdateServiceSessionRequest? request,
            ClaimsPrincipal currentUser,
            CancellationToken ct)
        {
            // 1. Kiểm tra quyền điều phối (Lễ tân, Quản lý, Admin).
            if (!currentUser.CanManageOperations())
                return Result<ServiceSessionResponse>.Forbidden("Chỉ lễ tân và quản lý mới có quyền điều chỉnh phiên dịch vụ.");

            if (id <= 0)
                return Result<ServiceSessionResponse>.BadRequest("id", "Mã phiên dịch vụ phải lớn hơn 0.");

            if (request is null)
                return Result<ServiceSessionResponse>.BadRequest("message", "Dữ liệu cập nhật phiên dịch vụ không được để trống.");

            // 2. Tìm phiên dịch vụ.
            var session = await serviceSessionRepository.GetByIdWithDetailsAsync(id, ct);
            if (session is null)
                return Result<ServiceSessionResponse>.NotFound("Không tìm thấy phiên dịch vụ.");

            var startUtc = NormalizeUtc(request.ExpectedStartAt);
            var endUtc = NormalizeUtc(request.ExpectedFinishAt);

            var errors = new Dictionary<string, string[]>();
            if (request.RoomId <= 0)
                errors["roomId"] = ["Mã phòng phải lớn hơn 0."];

            if (startUtc >= endUtc)
                errors["time"] = ["Thời gian bắt đầu dự kiến phải nhỏ hơn thời gian kết thúc dự kiến."];

            if (errors.Count > 0)
                return Result<ServiceSessionResponse>.ValidationFailed(errors);

            // 3. Chỉ cho phép điều chỉnh phiên đang ở trạng thái WAITING.
            if (session.Status != ServiceSessionStatus.WAITING)
                return Result<ServiceSessionResponse>.Conflict($"Chỉ có thể điều chỉnh phiên đang ở trạng thái WAITING (trạng thái hiện tại: {session.Status}).");

            // 4. Kiểm tra phòng đích tồn tại, khả dụng và không bị trùng phiên khác.
            var room = await serviceSessionRepository.GetRoomByIdAsync(request.RoomId, ct);
            if (room is null)
                return Result<ServiceSessionResponse>.Conflict("Phòng đích không tồn tại.");

            if (room.Status != RoomStatus.AVAILABLE)
                return Result<ServiceSessionResponse>.Conflict($"Phòng '{room.RoomName}' hiện không khả dụng ({room.Status}).");

            var hasRoomConflict = await serviceSessionRepository.HasOverlappingRoomSessionAsync(
                request.RoomId,
                startUtc,
                endUtc,
                session.Id,
                ct);
            if (hasRoomConflict)
                return Result<ServiceSessionResponse>.Conflict($"Phòng '{room.RoomName}' đã có phiên dịch vụ khác trùng khung giờ.");

            // 5. Kiểm tra lại trạng thái, ca làm và lịch trùng của từng nhân viên đang có phân công còn hiệu lực.
            var assignedStaff = session.StaffAssignments
                .Where(sa => sa.IsActive)
                .Select(sa => sa.Staff)
                .DistinctBy(st => st.Id)
                .OrderBy(st => st.Id)
                .ToList();
            var assignedStaffIds = assignedStaff.Select(st => st.Id).ToArray();

            if (assignedStaff.Count > 0)
            {
                var inactiveStaff = assignedStaff.Where(st => st.Status != StaffStatus.ACTIVE).ToList();
                if (inactiveStaff.Count > 0)
                {
                    return Result<ServiceSessionResponse>.Conflict(
                        $"Nhân viên đã phân công không còn hoạt động: {string.Join(", ", inactiveStaff.Select(st => st.FullName))}.");
                }

                var startDate = DateOnly.FromDateTime(startUtc);
                var endDate = DateOnly.FromDateTime(endUtc);
                if (startDate != endDate)
                {
                    return Result<ServiceSessionResponse>.Conflict(
                        "Khung giờ phiên mới vượt qua ranh giới ngày nên không đảm bảo ca làm việc cho nhân viên đã phân công.");
                }

                var coveringStaffIds = await serviceSessionRepository.GetStaffIdsWithCoveringShiftAsync(
                    assignedStaffIds,
                    startDate,
                    TimeOnly.FromDateTime(startUtc),
                    TimeOnly.FromDateTime(endUtc),
                    ct);
                var missingShiftStaff = assignedStaff.Where(st => !coveringStaffIds.Contains(st.Id)).ToList();
                if (missingShiftStaff.Count > 0)
                {
                    return Result<ServiceSessionResponse>.Conflict(
                        $"Nhân viên đã phân công không có ca làm việc bao phủ khung giờ mới: {string.Join(", ", missingShiftStaff.Select(st => st.FullName))}.");
                }

                var busyStaffIds = await serviceSessionRepository.GetStaffIdsWithOverlappingAssignmentAsync(
                    assignedStaffIds,
                    startUtc,
                    endUtc,
                    session.Id,
                    ct);
                if (busyStaffIds.Count > 0)
                {
                    var busyStaffNames = assignedStaff
                        .Where(st => busyStaffIds.Contains(st.Id))
                        .Select(st => st.FullName);
                    return Result<ServiceSessionResponse>.Conflict(
                        $"Nhân viên đã phân công bị trùng lịch với phiên khác trong khung giờ mới: {string.Join(", ", busyStaffNames)}.");
                }
            }

            // 6. Cập nhật trực tiếp phòng và khung giờ dự kiến mới trên chính phiên hiện tại.
            session.RoomId = request.RoomId;
            session.ExpectedStartAt = startUtc;
            session.ExpectedFinishAt = endUtc;
            session.UpdatedAt = DateTime.UtcNow;

            await serviceSessionRepository.SaveChangesAsync(ct);

            return Result<ServiceSessionResponse>.Success(ToResponse(session, room.RoomName, assignedStaffIds));
        }

        // OPS-08: Nhân viên được phân công bắt đầu thực hiện phiên dịch vụ (WAITING -> IN_PROGRESS).
        public async Task<Result<ServiceSessionResponse>> StartAsync(
            long id,
            ClaimsPrincipal currentUser,
            CancellationToken ct)
        {
            // 1. Lấy staffId từ JWT và đảm bảo tài khoản là nhân viên hợp lệ.
            if (!currentUser.HasStaffRole() || currentUser.GetStaffId() is not { } staffId)
                return Result<ServiceSessionResponse>.Forbidden("Bạn không được phân công thực hiện phiên này.");

            if (id <= 0)
                return Result<ServiceSessionResponse>.BadRequest("id", "Mã phiên dịch vụ phải lớn hơn 0.");

            // 2. Đọc phiên kèm thông tin phòng và danh sách phân công.
            var session = await serviceSessionRepository.GetByIdWithDetailsAsync(id, ct);
            if (session is null)
                return Result<ServiceSessionResponse>.NotFound("Không tìm thấy phiên dịch vụ.");

            // 3. Kiểm tra người gọi phải nằm trong phân công đang còn hiệu lực của phiên.
            var isAssignedStaff = session.StaffAssignments.Any(sa => sa.IsActive && sa.StaffId == staffId);
            if (!isAssignedStaff)
                return Result<ServiceSessionResponse>.Forbidden("Bạn không được phân công thực hiện phiên này.");

            var activeStaffIds = GetActiveStaffIds(session);
            var roomName = session.Room?.RoomName ?? string.Empty;

            // 4. Idempotent: Phiên đã ở trạng thái IN_PROGRESS -> giữ nguyên StartedAt cũ và trả 200 OK.
            if (session.Status == ServiceSessionStatus.IN_PROGRESS)
                return Result<ServiceSessionResponse>.Success(ToResponse(session, roomName, activeStaffIds));

            // 5. Kiểm tra trạng thái phiên phải là WAITING và đã có phòng hợp lệ.
            if (session.Status != ServiceSessionStatus.WAITING)
                return Result<ServiceSessionResponse>.Conflict($"Chỉ có thể bắt đầu phiên đang ở trạng thái WAITING (trạng thái hiện tại: {session.Status}).");

            if (session.RoomId <= 0 || session.Room is null)
                return Result<ServiceSessionResponse>.Conflict("Phiên dịch vụ chưa được bố trí phòng hợp lệ.");

            // 6. Ghi nhận bắt đầu phiên bằng giờ server một lần duy nhất.
            var now = DateTime.UtcNow;
            session.Status = ServiceSessionStatus.IN_PROGRESS;
            session.StartedAt ??= now;
            session.UpdatedAt = now;

            await serviceSessionRepository.SaveChangesAsync(ct);

            return Result<ServiceSessionResponse>.Success(ToResponse(session, roomName, activeStaffIds));
        }

        // OPS-11: Lấy danh sách các phiên dịch vụ có phân công còn hiệu lực của nhân viên đang đăng nhập.
        public async Task<Result<IReadOnlyList<MyServiceSessionResponse>>> GetMyAssignedSessionsAsync(
            ClaimsPrincipal currentUser,
            CancellationToken ct)
        {
            if (!currentUser.HasStaffRole() || currentUser.GetStaffId() is not { } staffId)
                return Result<IReadOnlyList<MyServiceSessionResponse>>.Forbidden("Chỉ tài khoản nhân viên mới có quyền xem danh sách công việc được giao.");

            var sessions = await serviceSessionRepository.GetActiveAssignedSessionsByStaffIdAsync(staffId, ct);
            return Result<IReadOnlyList<MyServiceSessionResponse>>.Success(sessions);
        }

        private static long[] GetActiveStaffIds(ServiceSession session) =>
            session.StaffAssignments
                .Where(sa => sa.IsActive)
                .Select(sa => sa.StaffId)
                .Distinct()
                .OrderBy(id => id)
                .ToArray();

        private static ServiceSessionResponse ToResponse(
            ServiceSession session,
            string roomName,
            IReadOnlyList<long> assignedStaffIds) =>
            new(
                session.Id,
                session.BookingDetailId,
                session.RoomId,
                roomName,
                session.Status,
                session.ExpectedStartAt,
                session.ExpectedFinishAt,
                session.StartedAt,
                session.FinishedAt,
                session.CreatedAt,
                session.UpdatedAt,
                assignedStaffIds);

        private static DateTime NormalizeUtc(DateTime value) => value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };
    }
}

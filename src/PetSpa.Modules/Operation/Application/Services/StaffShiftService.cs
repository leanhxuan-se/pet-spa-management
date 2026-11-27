using System.Globalization;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;
using PetSpa.Modules.Operation.Application.Abstractions;
using PetSpa.Modules.Operation.Domain.Entities;
using PetSpa.Modules.Operation.Domain.Enums;
using PetSpa.Modules.Operation.Endpoints.Requests;
using PetSpa.Modules.Operation.Endpoints.Responses;
using PetSpa.SharedKernel.Application.Abstractions;
using PetSpa.SharedKernel.Application.Authentication;

namespace PetSpa.Modules.Operation.Application.Services
{
    internal class StaffShiftService(IStaffShiftRepository staffShiftRepository)
    {
        private const int DefaultPageSize = 20;
        private const int MaxPageSize = 100;

        private static readonly Dictionary<string, StaffShiftStatus> AllowedStatuses = new(StringComparer.OrdinalIgnoreCase)
        {
            [nameof(StaffShiftStatus.SCHEDULED)] = StaffShiftStatus.SCHEDULED,
            [nameof(StaffShiftStatus.WORKING)] = StaffShiftStatus.WORKING,
            [nameof(StaffShiftStatus.COMPLETED)] = StaffShiftStatus.COMPLETED,
            [nameof(StaffShiftStatus.CANCELLED)] = StaffShiftStatus.CANCELLED,
        };


        public async Task<Result<List<StaffShiftResponse>>> GetMyStaffShiftsAsync(
            DateOnly date,
            ClaimsPrincipal currentUser,
            CancellationToken ct)
        {
            long? StaffId = currentUser.GetStaffId();
            var staffShifts = await staffShiftRepository.GetStaffShiftsAsync(date, StaffId.Value, ct);
            return Result<List<StaffShiftResponse>>.Success(staffShifts);
        }

        /// <summary>
        /// OPS-03 &amp; OPS-03A: Lấy danh sách ca làm có lọc và phân trang bằng cursor.
        /// </summary>
        public async Task<Result<PagedStaffShiftResponse>> GetPageAsync(
            DateOnly? date,
            long? staffId,
            string? status,
            string? cursor,
            int? pageSize,
            ClaimsPrincipal currentUser,
            CancellationToken ct)
        {
            if (!currentUser.CanManageStaff())
                return Result<PagedStaffShiftResponse>.Forbidden("Bạn không có quyền xem danh sách ca làm.");

            var errors = new Dictionary<string, string[]>();
            var effectivePageSize = pageSize ?? DefaultPageSize;
            StaffShiftStatus? parsedStatus = null;
            StaffShiftCursor? after = null;

            if (date is { } day && (day == DateOnly.MinValue || day == DateOnly.MaxValue))
                errors["date"] = ["Ngày làm việc không hợp lệ."];

            if (staffId.HasValue && staffId.Value <= 0)
                errors["staffId"] = ["Mã nhân viên phải lớn hơn 0."];

            if (status is not null)
            {
                if (!TryParseShiftStatus(status, out var validStatus))
                    errors["status"] = ["Trạng thái ca làm không hợp lệ. Chỉ chấp nhận: SCHEDULED, WORKING, COMPLETED, CANCELLED."];
                else
                    parsedStatus = validStatus;
            }

            if (pageSize.HasValue && (pageSize.Value <= 0 || pageSize.Value > MaxPageSize))
                errors["pageSize"] = [$"Số bản ghi mỗi trang phải từ 1 đến {MaxPageSize}."];

            if (cursor is not null && !TryDecodeCursor(cursor, out after))
                errors["cursor"] = ["Cursor không hợp lệ."];

            if (errors.Count > 0)
                return Result<PagedStaffShiftResponse>.ValidationFailed(errors);

            var rows = await staffShiftRepository.GetPageAsync(
                date,
                staffId,
                parsedStatus,
                after,
                effectivePageSize + 1,
                ct);

            var items = rows.Count > effectivePageSize
                ? rows.Take(effectivePageSize).ToArray()
                : rows;

            var nextCursor = rows.Count > effectivePageSize
                ? EncodeCursor(items[^1])
                : null;

            return Result<PagedStaffShiftResponse>.Success(new PagedStaffShiftResponse(items, nextCursor));
        }

        /// <summary>
        /// OPS-03B: Tạo ca làm mới ở trạng thái SCHEDULED.
        /// </summary>
        public async Task<Result<StaffShiftResponse>> CreateAsync(
            CreateStaffShiftRequest? request,
            ClaimsPrincipal currentUser,
            CancellationToken ct)
        {
            if (!currentUser.CanManageStaff())
                return Result<StaffShiftResponse>.Forbidden("Bạn không có quyền tạo ca làm.");

            if (request is null)
                return Result<StaffShiftResponse>.BadRequest("message", "Dữ liệu tạo ca làm không được để trống.");

            var errors = new Dictionary<string, string[]>();
            if (request.StaffId <= 0)
                errors["staffId"] = ["Mã nhân viên phải lớn hơn 0."];

            ValidateShiftDateAndTime(request.ShiftDate, request.StartTime, request.EndTime, errors);

            if (errors.Count > 0)
                return Result<StaffShiftResponse>.ValidationFailed(errors);

            var staffInfo = await staffShiftRepository.GetStaffInfoAsync(request.StaffId, ct);
            if (staffInfo is null)
                return Result<StaffShiftResponse>.NotFound("Không tìm thấy nhân viên.");

            if (staffInfo.Status != StaffStatus.ACTIVE)
                return Result<StaffShiftResponse>.UnActive("staffId", "Chỉ có thể tạo ca làm cho nhân viên đang hoạt động (ACTIVE).");

            var hasOverlap = await staffShiftRepository.HasOverlappingShiftAsync(
                request.StaffId,
                request.ShiftDate,
                request.StartTime,
                request.EndTime,
                excludeShiftId: null,
                ct);

            if (hasOverlap)
                return Result<StaffShiftResponse>.Conflict("Nhân viên đã có ca làm trùng khung giờ trong ngày này.");

            var now = DateTime.UtcNow;
            var shift = new StaffShift
            {
                StaffId = request.StaffId,
                ShiftDate = request.ShiftDate,
                StartTime = request.StartTime,
                EndTime = request.EndTime,
                Status = StaffShiftStatus.SCHEDULED,
                StartedAt = null,
                FinishedAt = null,
                CreatedAt = now,
                UpdatedAt = now
            };

            await staffShiftRepository.AddAsync(shift, ct);
            await staffShiftRepository.SaveChangesAsync(ct);

            return Result<StaffShiftResponse>.Created(ToResponse(shift, staffInfo.FullName));
        }

        /// <summary>
        /// OPS-03C: Điều chỉnh ngày hoặc khung giờ của ca làm đang ở trạng thái SCHEDULED.
        /// </summary>
        public async Task<Result<StaffShiftResponse>> UpdateAsync(
            long id,
            UpdateStaffShiftRequest? request,
            ClaimsPrincipal currentUser,
            CancellationToken ct)
        {
            if (!currentUser.CanManageStaff())
                return Result<StaffShiftResponse>.Forbidden("Bạn không có quyền điều chỉnh ca làm.");

            if (id <= 0)
                return Result<StaffShiftResponse>.BadRequest("id", "Mã ca làm phải lớn hơn 0.");

            if (request is null)
                return Result<StaffShiftResponse>.BadRequest("message", "Dữ liệu điều chỉnh ca làm không được để trống.");

            var shift = await staffShiftRepository.GetByIdWithStaffAsync(id, ct);
            if (shift is null)
                return Result<StaffShiftResponse>.NotFound("Không tìm thấy ca làm.");

            var errors = new Dictionary<string, string[]>();
            ValidateShiftDateAndTime(request.ShiftDate, request.StartTime, request.EndTime, errors);
            if (errors.Count > 0)
                return Result<StaffShiftResponse>.ValidationFailed(errors);

            if (shift.Status != StaffShiftStatus.SCHEDULED)
            {
                var reason = shift.Status == StaffShiftStatus.WORKING
                    ? "Ca làm đang thực hiện (WORKING) không thể điều chỉnh để giữ nguyên mốc giờ bắt đầu thực tế."
                    : $"Chỉ có thể điều chỉnh ca làm ở trạng thái SCHEDULED (trạng thái hiện tại: {shift.Status}).";
                return Result<StaffShiftResponse>.Conflict(reason);
            }

            var hasOverlap = await staffShiftRepository.HasOverlappingShiftAsync(
                shift.StaffId,
                request.ShiftDate,
                request.StartTime,
                request.EndTime,
                excludeShiftId: shift.Id,
                ct);

            if (hasOverlap)
                return Result<StaffShiftResponse>.Conflict("Nhân viên đã có ca làm khác trùng khung giờ trong ngày này.");

            var oldStartUtc = shift.ShiftDate.ToDateTime(shift.StartTime, DateTimeKind.Utc);
            var oldEndUtc = shift.ShiftDate.ToDateTime(shift.EndTime, DateTimeKind.Utc);
            var newStartUtc = request.ShiftDate.ToDateTime(request.StartTime, DateTimeKind.Utc);
            var newEndUtc = request.ShiftDate.ToDateTime(request.EndTime, DateTimeKind.Utc);

            var conflictingSessionIds = await staffShiftRepository.GetConflictingSessionIdsForShiftUpdateAsync(
                shift.StaffId,
                oldStartUtc,
                oldEndUtc,
                newStartUtc,
                newEndUtc,
                ct);

            if (conflictingSessionIds.Count > 0)
            {
                return Result<StaffShiftResponse>.Conflict(
                    $"Khung giờ ca làm mới không còn bao phủ đầy đủ các phiên dịch vụ đã phân công (mã phiên: {string.Join(", ", conflictingSessionIds)}). Vui lòng bố trí lại phiên trước khi điều chỉnh ca.");
            }

            shift.ShiftDate = request.ShiftDate;
            shift.StartTime = request.StartTime;
            shift.EndTime = request.EndTime;
            shift.UpdatedAt = DateTime.UtcNow;

            await staffShiftRepository.SaveChangesAsync(ct);

            return Result<StaffShiftResponse>.Success(ToResponse(shift));
        }

        /// <summary>
        /// OPS-03D: Hủy mềm ca làm (Soft Cancel) bằng cách đặt Status = CANCELLED.
        /// </summary>
        public async Task<Result<bool>> CancelAsync(
            long id,
            ClaimsPrincipal currentUser,
            CancellationToken ct)
        {
            if (!currentUser.CanManageStaff())
                return Result<bool>.Forbidden("Bạn không có quyền hủy ca làm.");

            if (id <= 0)
                return Result<bool>.BadRequest("id", "Mã ca làm phải lớn hơn 0.");

            var shift = await staffShiftRepository.GetByIdAsync(id, ct);
            if (shift is null)
                return Result<bool>.NotFound("Không tìm thấy ca làm.");

            if (shift.Status == StaffShiftStatus.CANCELLED)
                return Result<bool>.NoContent();

            if (shift.Status == StaffShiftStatus.WORKING)
                return Result<bool>.Conflict("Ca làm đang thực hiện (WORKING) không thể hủy; vui lòng kết thúc ca để giữ giờ thực tế.");

            if (shift.Status == StaffShiftStatus.COMPLETED)
                return Result<bool>.Conflict("Ca làm đã hoàn tất (COMPLETED), không thể hủy.");

            var shiftStartUtc = shift.ShiftDate.ToDateTime(shift.StartTime, DateTimeKind.Utc);
            var shiftEndUtc = shift.ShiftDate.ToDateTime(shift.EndTime, DateTimeKind.Utc);

            var hasActiveAssignments = await staffShiftRepository.HasActiveAssignedSessionsInWindowAsync(
                shift.StaffId,
                shiftStartUtc,
                shiftEndUtc,
                ct);

            if (hasActiveAssignments)
                return Result<bool>.Conflict("Ca vẫn còn phiên phân công, chưa thể hủy.");

            shift.Status = StaffShiftStatus.CANCELLED;
            shift.UpdatedAt = DateTime.UtcNow;

            await staffShiftRepository.SaveChangesAsync(ct);

            return Result<bool>.NoContent();
        }

        /// <summary>
        /// OPS-03E: Ghi nhận vào ca (SCHEDULED -> WORKING).
        /// </summary>
        public async Task<Result<StaffShiftResponse>> StartAsync(
            long id,
            ClaimsPrincipal currentUser,
            CancellationToken ct)
        {
            if (!currentUser.HasStaffRole())
                return Result<StaffShiftResponse>.Forbidden("Bạn không có quyền ghi nhận vào ca.");

            if (id <= 0)
                return Result<StaffShiftResponse>.BadRequest("id", "Mã ca làm phải lớn hơn 0.");

            var shift = await staffShiftRepository.GetByIdWithStaffAsync(id, ct);
            if (shift is null)
                return Result<StaffShiftResponse>.NotFound("Không tìm thấy ca làm.");

            if (!currentUser.CanRecordStaffShift(shift.StaffId))
                return Result<StaffShiftResponse>.Forbidden("Bạn không có quyền ghi nhận vào ca cho ca làm này.");

            if (shift.Status == StaffShiftStatus.WORKING)
                return Result<StaffShiftResponse>.Success(ToResponse(shift));

            if (shift.Status != StaffShiftStatus.SCHEDULED)
                return Result<StaffShiftResponse>.Conflict($"Không thể vào ca khi ca làm đang ở trạng thái {shift.Status}.");

            var now = DateTime.UtcNow;
            shift.Status = StaffShiftStatus.WORKING;
            shift.StartedAt ??= now;
            shift.UpdatedAt = now;

            await staffShiftRepository.SaveChangesAsync(ct);

            return Result<StaffShiftResponse>.Success(ToResponse(shift));
        }

        /// <summary>
        /// OPS-03E: Ghi nhận kết thúc ca (WORKING -> COMPLETED).
        /// </summary>
        public async Task<Result<StaffShiftResponse>> FinishAsync(
            long id,
            ClaimsPrincipal currentUser,
            CancellationToken ct)
        {
            if (!currentUser.HasStaffRole())
                return Result<StaffShiftResponse>.Forbidden("Bạn không có quyền ghi nhận kết thúc ca.");

            if (id <= 0)
                return Result<StaffShiftResponse>.BadRequest("id", "Mã ca làm phải lớn hơn 0.");

            var shift = await staffShiftRepository.GetByIdWithStaffAsync(id, ct);
            if (shift is null)
                return Result<StaffShiftResponse>.NotFound("Không tìm thấy ca làm.");

            if (!currentUser.CanRecordStaffShift(shift.StaffId))
                return Result<StaffShiftResponse>.Forbidden("Bạn không có quyền ghi nhận kết thúc ca cho ca làm này.");

            if (shift.Status == StaffShiftStatus.COMPLETED)
                return Result<StaffShiftResponse>.Success(ToResponse(shift));

            if (shift.Status != StaffShiftStatus.WORKING)
                return Result<StaffShiftResponse>.Conflict($"Chỉ có thể kết thúc ca đang ở trạng thái WORKING (trạng thái hiện tại: {shift.Status}).");

            var hasInProgressSessions = await staffShiftRepository.HasInProgressAssignedSessionsAsync(shift.StaffId, ct);
            if (hasInProgressSessions)
                return Result<StaffShiftResponse>.Conflict("Còn phiên dịch vụ đang làm, chưa thể kết thúc ca.");

            var now = DateTime.UtcNow;
            var finishedAt = shift.StartedAt.HasValue && now < shift.StartedAt.Value
                ? shift.StartedAt.Value
                : now;

            shift.Status = StaffShiftStatus.COMPLETED;
            shift.FinishedAt ??= finishedAt;
            shift.UpdatedAt = now;

            await staffShiftRepository.SaveChangesAsync(ct);

            return Result<StaffShiftResponse>.Success(ToResponse(shift));
        }

        private static void ValidateShiftDateAndTime(
            DateOnly shiftDate,
            TimeOnly startTime,
            TimeOnly endTime,
            Dictionary<string, string[]> errors)
        {
            if (shiftDate == DateOnly.MinValue || shiftDate == DateOnly.MaxValue)
                errors["shiftDate"] = ["Ngày làm việc không hợp lệ."];

            if (startTime >= endTime)
                errors["time"] = ["Giờ bắt đầu phải nhỏ hơn giờ kết thúc trong cùng một ngày."];
        }

        private static bool TryParseShiftStatus(string raw, out StaffShiftStatus status)
        {
            status = default;
            return !string.IsNullOrWhiteSpace(raw) && AllowedStatuses.TryGetValue(raw.Trim(), out status);
        }

        private static string EncodeCursor(StaffShiftResponse item)
        {
            var payload = string.Create(
                CultureInfo.InvariantCulture,
                $"{item.ShiftDate:yyyy-MM-dd}|{item.StartTime:HH:mm:ss.fffffff}|{item.Id}");
            return WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(payload));
        }

        private static bool TryDecodeCursor(string value, out StaffShiftCursor? cursor)
        {
            cursor = null;
            if (value.Length is 0 or > 256)
                return false;

            try
            {
                var decoded = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(value));
                var parts = decoded.Split('|');
                if (parts.Length != 3)
                    return false;

                if (!DateOnly.TryParseExact(parts[0], "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var shiftDate)
                    || shiftDate == DateOnly.MinValue
                    || shiftDate == DateOnly.MaxValue)
                {
                    return false;
                }

                if (!TimeOnly.TryParseExact(parts[1], "HH:mm:ss.fffffff", CultureInfo.InvariantCulture, DateTimeStyles.None, out var startTime))
                    return false;

                if (!long.TryParse(parts[2], NumberStyles.None, CultureInfo.InvariantCulture, out var id) || id <= 0)
                    return false;

                cursor = new StaffShiftCursor(shiftDate, startTime, id);
                return true;
            }
            catch (Exception ex) when (ex is FormatException or ArgumentException)
            {
                return false;
            }
        }

        

        private static StaffShiftResponse ToResponse(StaffShift shift, string? staffName = null) =>
            new(
                shift.Id,
                shift.StaffId,
                staffName ?? shift.Staff?.FullName ?? string.Empty,
                shift.ShiftDate,
                shift.StartTime,
                shift.EndTime,
                shift.StartedAt,
                shift.FinishedAt,
                shift.Status,
                shift.CreatedAt,
                shift.UpdatedAt);
        
    }
}

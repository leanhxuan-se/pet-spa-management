using System.Security.Claims;
using Microsoft.EntityFrameworkCore.Storage;
using PetSpa.Modules.Billing.Contracts;
using PetSpa.Modules.Booking.Contracts;
using PetSpa.Modules.Booking.Domain.Enums;
using PetSpa.Modules.Operation.Application.Abstractions;
using PetSpa.Modules.Operation.Domain.Entities;
using PetSpa.Modules.Operation.Domain.Enums;
using PetSpa.Modules.Operation.Endpoints.Requests;
using PetSpa.Modules.Operation.Endpoints.Responses;
using PetSpa.SharedKernel.Application.Abstractions;
using PetSpa.SharedKernel.Application.Authentication;

namespace PetSpa.Modules.Operation.Application.Services;

internal sealed class ServiceSessionGroupService(
    IServiceSessionRepository sessions, IBookingModule booking, IBillingModule billing)
{
    public async Task<Result<ServiceSessionGroupResponse>> CreateAsync(CreateServiceSessionGroupRequest request,
        ClaimsPrincipal currentUser, CancellationToken ct)
    {
        if (!currentUser.CanManageOperations())
            return Result<ServiceSessionGroupResponse>.Forbidden("Chỉ nhân sự điều phối được bàn giao lịch.");

        if (request.BookingId <= 0 || request.Items is not { Count: > 0 })
            return Result<ServiceSessionGroupResponse>.BadRequest("bookingId", "Cần mã lịch hợp lệ và ít nhất một dòng dịch vụ.");

        var group = new List<ServiceSession>();
        var detailIds = new HashSet<long>();
        foreach (var item in request.Items)
        {
            if (item is null || item.BookingDetailId <= 0 || item.RoomId <= 0
                || item.ExpectedStartAt is not { } start || item.ExpectedFinishAt is not { } finish)
                return Result<ServiceSessionGroupResponse>.BadRequest("items", "Mỗi dòng cần mã dịch vụ, mã phòng và hai mốc giờ hợp lệ.");

            var startUtc = NormalizeUtc(start);
            var finishUtc = NormalizeUtc(finish);
            if (startUtc >= finishUtc || !detailIds.Add(item.BookingDetailId))
                return Result<ServiceSessionGroupResponse>.BadRequest("items", "Khoảng giờ phải tăng dần và mã dòng dịch vụ không được trùng.");

            group.Add(new ServiceSession
            {
                BookingId = request.BookingId,
                BookingDetailId = item.BookingDetailId,
                RoomId = item.RoomId,
                ExpectedStartAt = startUtc,
                ExpectedFinishAt = finishUtc
            });
        }

        await using var transaction = await sessions.BeginWriteAsync(ct);
        var dbTransaction = transaction.GetDbTransaction();
        var candidate = await booking.GetHandoffCandidateAsync(request.BookingId, dbTransaction, ct);
        if (candidate.Status == BookingContractStatus.NotFound)
            return Result<ServiceSessionGroupResponse>.NotFound(candidate.Message!);
        if (candidate.Value is not { } handoff || !detailIds.SetEquals(handoff.Details.Select(detail => detail.BookingDetailId)))
            return Result<ServiceSessionGroupResponse>.Conflict(candidate.Message ?? "Cần bàn giao đúng và đủ các dòng của lịch.");

        if (handoff.SessionsCreatedAt is { } createdAt)
        {
            var existing = await sessions.GetByBookingIdsAsync([request.BookingId], ct);
            if (!detailIds.SetEquals(existing.Select(session => session.BookingDetailId)))
                return Result<ServiceSessionGroupResponse>.Conflict("Nhóm phiên đã bàn giao thiếu dữ liệu.");

            return Result<ServiceSessionGroupResponse>.Success(ToResponse(request.BookingId, createdAt, existing));
        }

        if (await sessions.HasSessionsForDetailsAsync(detailIds.ToArray(), ct))
            return Result<ServiceSessionGroupResponse>.Conflict("Đã có phiên cho dòng dịch vụ. Cần đối chiếu dữ liệu bàn giao.");

        var roomNames = new Dictionary<long, string>();
        foreach (var roomPlan in group.GroupBy(session => session.RoomId))
        {
            var room = await sessions.GetRoomByIdAsync(roomPlan.Key, ct);
            if (room is null || room.Status != RoomStatus.AVAILABLE)
                return Result<ServiceSessionGroupResponse>.Conflict($"Phòng {roomPlan.Key} không khả dụng.");

            roomNames.Add(room.Id, room.RoomName);
            DateTime? previousFinish = null;
            foreach (var session in roomPlan.OrderBy(session => session.ExpectedStartAt))
            {
                var startUtc = session.ExpectedStartAt!.Value;
                var finishUtc = session.ExpectedFinishAt!.Value;
                if (startUtc < previousFinish ||
                    await sessions.HasOverlappingRoomSessionAsync(room.Id, startUtc, finishUtc, 0, ct))
                    return Result<ServiceSessionGroupResponse>.Conflict($"Phòng '{room.RoomName}' có phiên trùng khung giờ.");

                previousFinish = finishUtc;
            }
        }

        var now = NormalizeUtc(DateTime.UtcNow);
        foreach (var session in group)
        {
            session.CreatedAt = now;
            session.UpdatedAt = now;
        }
        sessions.AddRange(group);
        await sessions.SaveChangesAsync(ct);
        var marked = await booking.MarkSessionsCreatedAsync(request.BookingId, detailIds, dbTransaction, ct);
        if (marked.Value is not { } saved)
            return Result<ServiceSessionGroupResponse>.Conflict(marked.Message ?? "Không thể ghi nhận bàn giao lịch.");

        await transaction.CommitAsync(ct);
        var createdSessions = group.OrderBy(session => session.BookingDetailId)
            .Select(session => ServiceSessionService.ToResponse(session, roomNames[session.RoomId], [])).ToArray();
        return Result<ServiceSessionGroupResponse>.Created(new(request.BookingId, saved.SessionsCreatedAt, createdSessions));
    }

    public async Task<Result<CurrentServiceSessionPage>> GetCurrentAsync(long? afterBookingId, int? pageSize,
        ClaimsPrincipal currentUser, CancellationToken ct)
    {
        if (!currentUser.CanManageOperations())
            return Result<CurrentServiceSessionPage>.Forbidden("Chỉ nhân sự điều phối được xem nhóm phiên hiện tại.");

        if (afterBookingId is <= 0 || pageSize is < 1 or > 100)
            return Result<CurrentServiceSessionPage>.BadRequest("pagination", "Mốc phân trang phải dương và pageSize trong khoảng 1–100.");

        var size = pageSize ?? 20;
        var cursor = afterBookingId ?? 0;
        var candidates = new List<BookingSessionGroup>(size + 1);
        while (candidates.Count <= size)
        {
            var batch = await booking.GetCurrentSessionGroupsAsync(cursor, size + 1, ct);
            if (batch.Count == 0)
                break;

            var bookingIds = batch.Select(group => group.BookingId).ToArray();
            var invoiced = (await billing.GetBookingIdsWithActiveInvoicesAsync(bookingIds, ct)).ToHashSet();
            candidates.AddRange(batch.Where(group => !invoiced.Contains(group.BookingId)));
            cursor = batch[^1].BookingId;
            if (batch.Count < size + 1)
                break;
        }

        var page = candidates.Take(size).ToArray();
        var nextCursor = candidates.Count > size ? page[^1].BookingId : (long?)null;
        if (page.Length == 0)
            return Result<CurrentServiceSessionPage>.Success(new([], null));

        var rows = await sessions.GetByBookingIdsAsync(page.Select(group => group.BookingId).ToArray(), ct);
        var byBooking = rows.ToLookup(session => session.BookingId);
        var items = page.Select(group => ToCurrentGroup(group, byBooking[group.BookingId].ToArray())).ToArray();
        return Result<CurrentServiceSessionPage>.Success(new(items, nextCursor));
    }

    private static CurrentServiceSessionGroup ToCurrentGroup(BookingSessionGroup group, ServiceSession[] rows)
    {
        var waiting = rows.Count(session => session.Status == ServiceSessionStatus.WAITING);
        var inProgress = rows.Count(session => session.Status == ServiceSessionStatus.IN_PROGRESS);
        var completed = rows.Count(session => session.Status == ServiceSessionStatus.COMPLETED);
        var cancelled = rows.Count(session => session.Status == ServiceSessionStatus.CANCELLED);
        var ready = group.HandedOffDetailIds.Count > 0 && waiting == 0 && inProgress == 0
            && group.HandedOffDetailIds.Count == rows.Length
            && group.HandedOffDetailIds.ToHashSet().SetEquals(rows.Select(session => session.BookingDetailId));

        return new(group.BookingId, group.Status, group.SessionsCreatedAt, waiting, inProgress, completed, cancelled,
            ready && completed > 0 && group.Status == BookingStatus.CONFIRMED,
            ready && completed == 0 && group.Status == BookingStatus.CONFIRMED,
            ready && completed > 0 && group.Status == BookingStatus.COMPLETED,
            rows.Select(ServiceSessionService.ToResponse).ToArray());
    }

    private static ServiceSessionGroupResponse ToResponse(long bookingId, DateTime createdAt,
        IEnumerable<ServiceSession> rows) => new(bookingId, createdAt, rows.OrderBy(session => session.BookingDetailId)
            .Select(ServiceSessionService.ToResponse).ToArray());

    private static DateTime NormalizeUtc(DateTime value)
    {
        var utc = value.Kind == DateTimeKind.Local
            ? value.ToUniversalTime()
            : DateTime.SpecifyKind(value, DateTimeKind.Utc);
        return utc.AddTicks(-(utc.Ticks % 10));
    }
}

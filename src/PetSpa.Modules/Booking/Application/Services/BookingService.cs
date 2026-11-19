using System.Text.Json;
using Microsoft.AspNetCore.WebUtilities;
using PetSpa.Modules.Booking.Application.Abstractions;
using PetSpa.Modules.Booking.Application.Dtos;
using PetSpa.Modules.Booking.Domain.Entities;
using PetSpa.Modules.Booking.Domain.Enums;
using PetSpa.Modules.Booking.Domain.Exceptions;
using PetSpa.Modules.Customer.Contracts;
using PetSpa.Modules.Resource.Contracts;
using BookingEntity = PetSpa.Modules.Booking.Domain.Entities.Booking;

namespace PetSpa.Modules.Booking.Application.Services;

public sealed class BookingService(
    IBookingRepository repository, ICustomerModule customers, IResourceModule resources)
{
    public async Task<BookingResult<BookingDto>> GetByIdAsync(long id, BookingActor actor, CancellationToken ct)
    {
        if (!actor.IsFrontDesk && actor.CustomerId is null)
            return BookingResult<BookingDto>.Forbidden();

        var booking = await repository.GetByIdAsync(id, ct);
        if (booking is null)
            return BookingResult<BookingDto>.NotFound();

        return actor.CanAccess(booking.CustomerId)
            ? BookingResult<BookingDto>.Success(ToDto(booking))
            : BookingResult<BookingDto>.Forbidden();
    }

    public async Task<BookingResult<BookingPageDto>> GetPageAsync(
        BookingListQuery query, BookingActor actor, CancellationToken ct)
    {
        if (!actor.IsFrontDesk)
            return BookingResult<BookingPageDto>.Forbidden();

        var errors = new Dictionary<string, string[]>();
        var limit = query.PageSize ?? 25;
        var suppliedPhone = query.ContactPhone?.Trim();
        // Giữ khả năng tìm theo giá trị cũ lưu trong lịch nếu số đó có định dạng khác.
        var phone = NormalizePhone(suppliedPhone) ?? suppliedPhone;
        BookingCursor? after = null;

        if (query.Status.HasValue && !Enum.IsDefined(query.Status.Value))
            errors["status"] = ["Trạng thái booking không hợp lệ."];
        if (query.CustomerId <= 0)
            errors["customerId"] = ["Mã khách hàng phải lớn hơn 0."];
        if (query.Date is { } day && (day == DateOnly.MinValue || day == DateOnly.MaxValue))
            errors["date"] = ["Ngày hẹn không hợp lệ."];
        if (phone is { Length: 0 or > 30 })
            errors["contactPhone"] = ["Điện thoại liên hệ không hợp lệ."];
        if (limit is < 1 or > 100)
            errors["pageSize"] = ["Số lịch mỗi trang phải từ 1 đến 100."];
        if (query.Cursor is not null && !TryReadCursor(query.Cursor, out after))
            errors["cursor"] = ["Cursor không hợp lệ."];
        if (errors.Count > 0)
            return BookingResult<BookingPageDto>.Invalid(errors);

        var bookings = await repository.ReadPageAsync(query with { ContactPhone = phone }, after, limit + 1, ct);
        var items = bookings.Take(limit).Select(ToDto).ToArray();
        var next = bookings.Count > limit
            ? WebEncoders.Base64UrlEncode(JsonSerializer.SerializeToUtf8Bytes(
                new BookingCursor(StatusOrder(items[^1].Status), items[^1].BookingTime, items[^1].Id)))
            : null;
        return BookingResult<BookingPageDto>.Success(new(items, next));
    }

    public async Task<BookingResult<BookingDto>> CreateAsync(
        CreateBookingCommand command, BookingActor actor, CancellationToken ct)
    {
        if (!actor.IsFrontDesk)
            return BookingResult<BookingDto>.Forbidden();

        var errors = new Dictionary<string, string[]>();
        if (command.CustomerId <= 0)
            errors["customerId"] = ["Mã khách hàng phải lớn hơn 0."];
        if (command.PetId <= 0)
            errors["petId"] = ["Mã thú cưng phải lớn hơn 0."];
        ValidateUtc(command.BookingTime, errors);
        ValidateServices(command.ServiceOptionIds, errors);
        if (errors.Count > 0)
            return BookingResult<BookingDto>.Invalid(errors);

        var customer = await customers.GetBookingCustomerAsync(command.CustomerId, command.PetId, ct);
        if (customer is null)
            errors["petId"] = ["Thú cưng phải thuộc đúng khách hàng và cả hai hồ sơ phải ACTIVE."];

        var options = await GetOptionsAsync(command.ServiceOptionIds!, errors, ct);
        if (errors.Count > 0)
            return BookingResult<BookingDto>.Invalid(errors);

        var now = BookingEntity.UtcNow();
        var booking = new BookingEntity
        {
            CustomerId = command.CustomerId,
            PetId = command.PetId,
            BookingTime = BookingEntity.AtDatabasePrecision(command.BookingTime),
            ContactName = customer!.FullName,
            ContactPhone = customer.Phone,
            Note = command.Note,
            CreatedAt = now,
            UpdatedAt = now,
            BookingDetails = command.ServiceOptionIds!.Select(id => new BookingDetail
            {
                ServiceOptionsId = id,
                UnitPrice = options[id].UnitPrice
            }).ToList()
        };
        repository.Add(booking);
        await repository.SaveChangesAsync(ct);
        return BookingResult<BookingDto>.Success(ToDto(booking));
    }

    public async Task<BookingResult<BookingDto>> ReplaceServicesAsync(
        long id, ReplaceBookingServicesCommand command, BookingActor actor, CancellationToken ct)
    {
        try
        {
            return await UpdateLockedAsync(id, actor, async (booking, token) =>
            {
                if (booking.Status is not (BookingStatus.PENDING or BookingStatus.CONFIRMED)
                    || booking.SessionsCreatedAt.HasValue || booking.LegacyHandoffNeedsReview)
                    return BookingResult<BookingDto>.Conflict("Chỉ được đổi dịch vụ cho lịch PENDING hoặc CONFIRMED trước bàn giao.");

                var errors = new Dictionary<string, string[]>();
                ValidateServices(command.ServiceOptionIds, errors);
                if (errors.Count > 0)
                    return BookingResult<BookingDto>.Invalid(errors);

                var selectedIds = command.ServiceOptionIds!.ToHashSet();
                var removed = booking.BookingDetails.Where(d => !selectedIds.Contains(d.ServiceOptionsId)).ToArray();
                if (removed.Any(d => d.Status is BookingDetailStatus.WORKING or BookingDetailStatus.COMPLETED))
                    return BookingResult<BookingDto>.Conflict("Không thể bỏ dịch vụ đang thực hiện hoặc đã hoàn thành.");

                var options = await GetOptionsAsync(command.ServiceOptionIds!, errors, token);
                if (errors.Count > 0)
                    return BookingResult<BookingDto>.Invalid(errors);

                foreach (var detail in removed)
                {
                    repository.RemoveDetail(detail);
                    booking.BookingDetails.Remove(detail);
                }
                var retainedIds = booking.BookingDetails.Select(d => d.ServiceOptionsId).ToHashSet();
                foreach (var optionId in command.ServiceOptionIds!.Where(optionId => !retainedIds.Contains(optionId)))
                {
                    booking.BookingDetails.Add(new BookingDetail
                    {
                        ServiceOptionsId = optionId,
                        UnitPrice = options[optionId].UnitPrice
                    });
                }
                return await SaveAsync(booking, BookingEntity.UtcNow(), token);
            }, ct);
        }
        catch (BookingDetailInUseException exception)
        {
            return BookingResult<BookingDto>.Conflict(exception.Message);
        }
    }

    public Task<BookingResult<BookingDto>> UpdateAsync(
        long id, UpdateBookingCommand command, BookingActor actor, CancellationToken ct)
        => UpdateLockedAsync(id, actor, async (booking, token) =>
        {
            if (booking.Status != BookingStatus.PENDING || booking.LegacyHandoffNeedsReview)
                return BookingResult<BookingDto>.Conflict("Chỉ được sửa thông tin lịch PENDING trước bàn giao.");

            var errors = new Dictionary<string, string[]>();
            var contactName = command.ContactName?.Trim();
            var contactPhone = NormalizePhone(command.ContactPhone);
            if (command.HasBookingTime)
                ValidateUtc(command.BookingTime, errors);
            if (command.HasContactName && (string.IsNullOrWhiteSpace(contactName) || contactName.Length > 150))
                errors["contactName"] = ["Tên liên hệ phải có từ 1 đến 150 ký tự."];
            if (command.HasContactPhone && contactPhone is null)
                errors["contactPhone"] = ["Điện thoại liên hệ không hợp lệ."];
            if (!command.HasBookingTime && !command.HasContactName && !command.HasContactPhone && !command.HasNote)
                errors["body"] = ["Cần gửi ít nhất một trường cần sửa."];
            if (errors.Count > 0)
                return BookingResult<BookingDto>.Invalid(errors);

            if (command.HasBookingTime)
                booking.BookingTime = BookingEntity.AtDatabasePrecision(command.BookingTime!.Value);
            if (command.HasContactName)
                booking.ContactName = contactName;
            if (command.HasContactPhone)
                booking.ContactPhone = contactPhone;
            if (command.HasNote)
                booking.Note = command.Note;
            if (booking.ContactName is null || booking.ContactPhone is null)
            {
                var contact = await customers.GetBookingContactAsync(booking.CustomerId, token);
                booking.ContactName ??= contact?.FullName;
                booking.ContactPhone ??= contact?.Phone;
            }
            return await SaveAsync(booking, BookingEntity.UtcNow(), token);
        }, ct);

    public Task<BookingResult<BookingDto>> CancelAsync(long id, BookingActor actor, CancellationToken ct)
        => UpdateLockedAsync(id, actor, async (booking, token) =>
        {
            if (booking.Status == BookingStatus.CANCELLED)
                return BookingResult<BookingDto>.Success(ToDto(booking));
            if (booking.Status is not (BookingStatus.PENDING or BookingStatus.CONFIRMED)
                || booking.LegacyHandoffNeedsReview)
                return BookingResult<BookingDto>.Conflict("Lịch không đủ điều kiện hủy.");
            if (booking.SessionsCreatedAt.HasValue && (booking.Status != BookingStatus.CONFIRMED
                || !booking.HasCompleteHandoff() || booking.BookingDetails.Any(d => d.Status != BookingDetailStatus.CANCELLED)))
                return BookingResult<BookingDto>.Conflict("Chỉ được hủy sau bàn giao khi đủ dòng và mọi dòng đã hủy.");
            if (!booking.SessionsCreatedAt.HasValue
                && booking.BookingDetails.Any(d => d.Status is BookingDetailStatus.WORKING or BookingDetailStatus.COMPLETED))
                return BookingResult<BookingDto>.Conflict("Lịch có dòng đã thực hiện và cần đối soát phiên.");

            foreach (var detail in booking.BookingDetails)
                detail.Status = BookingDetailStatus.CANCELLED;
            booking.Status = BookingStatus.CANCELLED;
            var now = BookingEntity.UtcNow();
            booking.CancelledAt ??= now;
            return await SaveAsync(booking, now, token);
        }, ct);

    public Task<BookingResult<BookingDto>> ChangeStatusAsync(
        long id, ChangeBookingStatusCommand command, BookingActor actor, CancellationToken ct)
        => UpdateLockedAsync(id, actor, async (booking, token) =>
        {
            if (command.Status is not (BookingStatus.CONFIRMED or BookingStatus.COMPLETED))
                return BookingResult<BookingDto>.Invalid(new Dictionary<string, string[]>
                {
                    ["status"] = ["Giá trị cho phép là confirmed và completed."]
                });
            if (booking.Status == command.Status)
                return BookingResult<BookingDto>.Success(ToDto(booking));
            if (booking.LegacyHandoffNeedsReview)
                return BookingResult<BookingDto>.Conflict("Cần đối soát tập dịch vụ của lịch trước khi chuyển trạng thái.");

            var now = BookingEntity.UtcNow();
            if (command.Status == BookingStatus.CONFIRMED)
            {
                if (booking.Status != BookingStatus.PENDING
                    || booking.BookingDetails.Count == 0
                    || booking.BookingDetails.Any(d => d.Status != BookingDetailStatus.ASSIGNED))
                    return BookingResult<BookingDto>.Conflict("Chỉ được tiếp nhận lịch PENDING có dịch vụ hợp lệ.");

                var optionIds = booking.BookingDetails.Select(d => d.ServiceOptionsId).Distinct().ToArray();
                var options = await resources.GetActiveServiceOptionsAsync(optionIds, token);
                if (optionIds.Length != booking.BookingDetails.Count || options.Count != optionIds.Length)
                    return BookingResult<BookingDto>.Conflict("Lịch có tùy chọn dịch vụ không còn hợp lệ.");

                booking.CheckIn ??= now;
            }
            else if (booking.Status != BookingStatus.CONFIRMED || !booking.HasCompleteHandoff()
                || booking.BookingDetails.Any(d => d.Status is not (BookingDetailStatus.COMPLETED or BookingDetailStatus.CANCELLED))
                || booking.BookingDetails.All(d => d.Status != BookingDetailStatus.COMPLETED))
            {
                return BookingResult<BookingDto>.Conflict("Cần đủ dòng đã kết thúc và ít nhất một dòng COMPLETED để chốt lịch.");
            }

            booking.Status = command.Status;
            return await SaveAsync(booking, now, token);
        }, ct);

    private Task<BookingResult<BookingDto>> UpdateLockedAsync(
        long id, BookingActor actor, Func<BookingEntity, CancellationToken, Task<BookingResult<BookingDto>>> update,
        CancellationToken ct)
    {
        if (!actor.IsFrontDesk)
            return Task.FromResult(BookingResult<BookingDto>.Forbidden());

        return repository.InTransactionAsync(async token =>
        {
            var booking = await repository.GetForUpdateAsync(id, token);
            return booking is null ? BookingResult<BookingDto>.NotFound() : await update(booking, token);
        }, ct);
    }

    private async Task<BookingResult<BookingDto>> SaveAsync(BookingEntity booking, DateTime now, CancellationToken ct)
    {
        booking.UpdatedAt = now;
        await repository.SaveChangesAsync(ct);
        return BookingResult<BookingDto>.Success(ToDto(booking));
    }

    private async Task<Dictionary<long, BookableServiceOption>> GetOptionsAsync(
        IReadOnlyList<long> ids, Dictionary<string, string[]> errors, CancellationToken ct)
    {
        var options = (await resources.GetActiveServiceOptionsAsync(ids, ct)).ToDictionary(o => o.Id);
        var unavailable = ids.Where(id => !options.ContainsKey(id)).ToArray();
        if (unavailable.Length > 0)
            errors["serviceOptionIds"] = [$"Tùy chọn không tồn tại hoặc option/service/category không ACTIVE: {string.Join(", ", unavailable)}."];
        return options;
    }

    private static void ValidateUtc(DateTime? value, Dictionary<string, string[]> errors)
    {
        if (value is not { Kind: DateTimeKind.Utc } time || time == DateTime.MinValue || time == DateTime.MaxValue)
            errors["bookingTime"] = ["Giờ hẹn phải hợp lệ và ở UTC (hậu tố Z)."];
    }

    private static void ValidateServices(IReadOnlyList<long>? ids, Dictionary<string, string[]> errors)
    {
        if (ids is null || ids.Count == 0)
            errors["serviceOptionIds"] = ["Cần ít nhất một tùy chọn dịch vụ."];
        else if (ids.Any(id => id <= 0) || ids.Distinct().Count() != ids.Count)
            errors["serviceOptionIds"] = ["ID tùy chọn phải lớn hơn 0 và không được trùng."];
    }

    private static string? NormalizePhone(string? value)
    {
        var phone = value?.Trim();
        if (string.IsNullOrEmpty(phone))
            return null;
        var digits = phone[0] == '+' ? phone[1..] : phone;
        if (digits.Length is < 7 or > 15 || !digits.All(char.IsAsciiDigit))
            return null;
        return phone is ['+', '8', '4', '3' or '5' or '7' or '8' or '9', ..] && phone.Length == 12
            ? "0" + phone[3..] : phone;
    }

    private static int StatusOrder(BookingStatus status) => status switch
    {
        BookingStatus.COMPLETED => 0,
        BookingStatus.CONFIRMED => 1,
        BookingStatus.PAID => 2,
        BookingStatus.PENDING => 3,
        BookingStatus.CANCELLED => 4,
        _ => throw new ArgumentOutOfRangeException(nameof(status))
    };

    private static bool TryReadCursor(string value, out BookingCursor? cursor)
    {
        cursor = null;
        if (value.Length is 0 or > 256)
            return false;
        try
        {
            cursor = JsonSerializer.Deserialize<BookingCursor>(WebEncoders.Base64UrlDecode(value));
            return cursor is { StatusOrder: >= 0 and <= 4, Id: > 0 }
                && (cursor.BookingTime is null or { Kind: DateTimeKind.Utc });
        }
        catch (Exception exception) when (exception is FormatException or JsonException)
        {
            return false;
        }
    }

    private static BookingDto ToDto(BookingEntity booking)
        => new(booking.Id, booking.CustomerId, booking.PetId, booking.BookingTime,
            booking.CheckIn, booking.CheckOut, booking.Note, booking.Status, booking.CreatedAt, booking.UpdatedAt,
            booking.BookingDetails.OrderBy(d => d.Id).Select(d => new BookingDetailDto(
                d.Id, d.ServiceOptionsId, d.UnitPrice, d.Status)).ToArray(),
            booking.ContactName, booking.ContactPhone, booking.SessionsCreatedAt, booking.CancelledAt);
}

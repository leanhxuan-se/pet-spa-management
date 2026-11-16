// Xử lý nghiệp vụ và cung cấp dịch vụ BookingService.
using PetSpa.Modules.Booking.Application.Abstractions;
using PetSpa.Modules.Booking.Application.Dtos;
using PetSpa.Modules.Booking.Domain.Entities;
using PetSpa.Modules.Booking.Domain.Enums;
using PetSpa.Modules.Customer.Contracts;
using PetSpa.Modules.Resource.Contracts;
using BookingEntity = PetSpa.Modules.Booking.Domain.Entities.Booking;

namespace PetSpa.Modules.Booking.Application.Services;

public sealed class BookingService(
    IBookingRepository repository, ICustomerModule customers, IResourceModule resources)
{
    // Tìm đối tượng theo mã.
    public async Task<BookingResult<BookingDto>> GetByIdAsync(long id, BookingActor actor, CancellationToken ct)
    {
        // 1. Lấy thông tin lịch hẹn.
        var booking = await repository.GetByIdAsync(id, ct);
        if (booking is null) return BookingResult<BookingDto>.NotFound();
        // 2. Kiểm tra quyền truy cập của tài khoản.
        return actor.CanAccess(booking.CustomerId)
            ? BookingResult<BookingDto>.Success(ToDto(booking))
            : BookingResult<BookingDto>.Forbidden();
    }

    // Tạo đối tượng mới.
    public async Task<BookingResult<BookingDto>> CreateAsync(
        CreateBookingCommand command, BookingActor actor, CancellationToken ct)
    {
        // 1. Kiểm tra quyền truy cập của tài khoản.
        if (!actor.CanAccess(command.CustomerId)) return BookingResult<BookingDto>.Forbidden();
        // 2. Kiểm tra dữ liệu đầu vào.
        var errors = ValidateBooking(command.CustomerId, command.PetId, command.BookingTime);
        ValidateServices(command.ServiceOptionIds, errors);
        if (errors.Count > 0) return BookingResult<BookingDto>.Invalid(errors);

        // 3. Kiểm tra thú cưng và các dịch vụ liên quan.
        var options = await ValidateReferencesAsync(
            command.CustomerId, command.PetId, command.ServiceOptionIds!, errors, ct);
        if (errors.Count > 0) return BookingResult<BookingDto>.Invalid(errors);

        // 4. Tạo thông tin lịch hẹn mới.
        var booking = new BookingEntity
        {
            CustomerId = command.CustomerId, PetId = command.PetId,
            BookingTime = command.BookingTime, Note = command.Note,
            BookingDetails = command.ServiceOptionIds!.Select(id => new BookingDetail
            {
                ServiceOptionsId = id, UnitPrice = options[id].UnitPrice
            }).ToList()
        };
        // 5. Thêm dữ liệu mới để lưu.
        repository.Add(booking);
        // 6. Lưu các thay đổi.
        await repository.SaveChangesAsync(ct);
        // 7. Trả kết quả xử lý.
        return BookingResult<BookingDto>.Success(ToDto(booking));
    }

    // Thay danh sách dịch vụ của lịch hẹn.
    public async Task<BookingResult<BookingDto>> ReplaceServicesAsync(
        long id, ReplaceBookingServicesCommand command, BookingActor actor, CancellationToken ct)
    {
        // 1. Lấy thông tin lịch hẹn.
        var booking = await repository.GetByIdAsync(id, ct);
        if (booking is null) return BookingResult<BookingDto>.NotFound();
        // 2. Kiểm tra quyền truy cập của tài khoản.
        if (!actor.CanAccess(booking.CustomerId)) return BookingResult<BookingDto>.Forbidden();
        // 3. Kiểm tra trạng thái cho phép chỉnh sửa.
        if (!CanEditServices(booking))
            return BookingResult<BookingDto>.Conflict("Chỉ được đổi dịch vụ khi booking đang PENDING hoặc CONFIRMED.");

        var errors = new Dictionary<string, string[]>();
        ValidateServices(command.ServiceOptionIds, errors);
        if (errors.Count > 0) return BookingResult<BookingDto>.Invalid(errors);

        // 4. Xác định các dịch vụ được chọn và cần xóa.
        var selectedIds = command.ServiceOptionIds!.ToHashSet();
        var removed = booking.BookingDetails.Where(d => !selectedIds.Contains(d.ServiceOptionsId)).ToList();
        if (removed.Any(d => d.Status is BookingDetailStatus.WORKING or BookingDetailStatus.COMPLETED))
            return BookingResult<BookingDto>.Conflict("Không thể xóa dịch vụ đang thực hiện hoặc đã hoàn thành.");

        // Validate the complete replacement before modifying any tracked entity.
        var options = await ValidateReferencesAsync(
            booking.CustomerId, booking.PetId, command.ServiceOptionIds!, errors, ct);
        if (errors.Count > 0) return BookingResult<BookingDto>.Invalid(errors);

        // 5. Xóa các dịch vụ không còn được chọn.
        foreach (var detail in removed)
        {
            repository.RemoveDetail(detail);
            booking.BookingDetails.Remove(detail);
        }
        var retainedIds = booking.BookingDetails.Select(d => d.ServiceOptionsId).ToHashSet();
        // 6. Thêm các dịch vụ mới được chọn.
        foreach (var optionId in command.ServiceOptionIds!.Where(optionId => !retainedIds.Contains(optionId)))
        {
            booking.BookingDetails.Add(new BookingDetail
            {
                BookingId = booking.Id, Booking = booking,
                ServiceOptionsId = optionId, UnitPrice = options[optionId].UnitPrice
            });
        }
        booking.UpdatedAt = DateTime.UtcNow;
        // 7. Lưu các thay đổi.
        await repository.SaveChangesAsync(ct);
        // 8. Trả kết quả xử lý.
        return BookingResult<BookingDto>.Success(ToDto(booking));
    }

    // Cập nhật đối tượng.
    public async Task<BookingResult<BookingDto>> UpdateAsync(
        long id, UpdateBookingCommand command, BookingActor actor, CancellationToken ct)
    {
        // 1. Lấy thông tin lịch hẹn.
        var booking = await repository.GetByIdAsync(id, ct);
        if (booking is null) return BookingResult<BookingDto>.NotFound();
        // 2. Kiểm tra quyền truy cập của tài khoản.
        if (!actor.CanAccess(booking.CustomerId) || !actor.CanAccess(command.CustomerId))
            return BookingResult<BookingDto>.Forbidden();
        if (!actor.IsStaff && (command.Status != booking.Status || command.CheckIn != booking.CheckIn
            || command.CheckOut != booking.CheckOut))
            return BookingResult<BookingDto>.Forbidden();
        // 3. Kiểm tra trạng thái cho phép chỉnh sửa.
        if (!actor.IsStaff && !CanEditServices(booking))
            return BookingResult<BookingDto>.Conflict("Khách hàng chỉ được sửa booking đang PENDING hoặc CONFIRMED.");

        // 4. Kiểm tra dữ liệu đầu vào.
        var errors = ValidateBooking(command.CustomerId, command.PetId, command.BookingTime);
        if (!Enum.IsDefined(command.Status)) errors["Status"] = ["Trạng thái booking không hợp lệ."];
        if (command.CheckIn.HasValue) ValidateUtc(command.CheckIn.Value, "CheckIn", errors);
        if (command.CheckOut.HasValue) ValidateUtc(command.CheckOut.Value, "CheckOut", errors);
        if (command.CheckOut.HasValue && !command.CheckIn.HasValue)
            errors["CheckOut"] = ["Phải có thời gian check-in trước khi check-out."];
        if (command.CheckOut < command.CheckIn)
            errors["CheckOut"] = ["Check-out không được trước check-in."];
        if (errors.Count > 0) return BookingResult<BookingDto>.Invalid(errors);

        // Even a note/time-only update rechecks ownership and every retained option.
        var optionIds = booking.BookingDetails.Select(d => d.ServiceOptionsId).Distinct().ToArray();
        // 5. Kiểm tra thú cưng và các dịch vụ liên quan.
        await ValidateReferencesAsync(command.CustomerId, command.PetId, optionIds, errors, ct);
        if (errors.Count > 0) return BookingResult<BookingDto>.Invalid(errors);

        // 6. Cập nhật thông tin lịch hẹn.
        booking.CustomerId = command.CustomerId;
        booking.PetId = command.PetId;
        booking.BookingTime = command.BookingTime;
        booking.Note = command.Note;
        booking.Status = command.Status;
        booking.CheckIn = command.CheckIn;
        booking.CheckOut = command.CheckOut;
        booking.UpdatedAt = DateTime.UtcNow;
        // 7. Lưu các thay đổi.
        await repository.SaveChangesAsync(ct);
        // 8. Trả kết quả xử lý.
        return BookingResult<BookingDto>.Success(ToDto(booking));
    }

    // Kiểm tra thú cưng và dịch vụ liên quan.
    private async Task<Dictionary<long, BookableServiceOption>> ValidateReferencesAsync(
        long customerId, long petId, IReadOnlyCollection<long> ids,
        Dictionary<string, string[]> errors, CancellationToken ct)
    {
        // 1. Kiểm tra thú cưng thuộc khách hàng.
        if (!await customers.PetBelongsToCustomerAsync(petId, customerId, ct))
            errors["PetId"] = ["Thú cưng không tồn tại hoặc không thuộc khách hàng đã chọn."];

        // 2. Lấy các tùy chọn dịch vụ đang hoạt động.
        var options = (await resources.GetActiveServiceOptionsAsync(ids, ct)).ToDictionary(o => o.Id);
        var unavailable = ids.Where(id => !options.ContainsKey(id)).ToArray();
        if (unavailable.Length > 0)
            errors["ServiceOptionIds"] = [$"Tùy chọn không tồn tại hoặc option/service/category không ACTIVE: {string.Join(", ", unavailable)}."];
        return options;
    }

    // Kiểm tra dữ liệu lịch hẹn.
    private static Dictionary<string, string[]> ValidateBooking(long customerId, long petId, DateTime time)
    {
        // 1. Kiểm tra mã khách hàng và thú cưng.
        var errors = new Dictionary<string, string[]>();
        if (customerId <= 0) errors["CustomerId"] = ["CustomerId phải lớn hơn 0."];
        if (petId <= 0) errors["PetId"] = ["PetId phải lớn hơn 0."];
        // 2. Kiểm tra thời gian đặt lịch.
        ValidateUtc(time, "BookingTime", errors);
        return errors;
    }

    // Kiểm tra thời gian có múi giờ UTC.
    private static void ValidateUtc(DateTime time, string field, Dictionary<string, string[]> errors)
    {
        if (time == default || time.Kind != DateTimeKind.Utc)
            errors[field] = ["Thời gian phải hợp lệ và ở UTC (hậu tố Z)."];
    }

    // Kiểm tra danh sách dịch vụ.
    private static void ValidateServices(IReadOnlyList<long>? ids, Dictionary<string, string[]> errors)
    {
        if (ids is null || ids.Count == 0)
            errors["ServiceOptionIds"] = ["Booking phải có ít nhất một tùy chọn dịch vụ."];
        else if (ids.Any(id => id <= 0) || ids.Distinct().Count() != ids.Count)
            errors["ServiceOptionIds"] = ["ID tùy chọn phải lớn hơn 0 và không được trùng."];
    }

    // Kiểm tra lịch hẹn có thể đổi dịch vụ hay không.
    private static bool CanEditServices(BookingEntity booking)
        => booking.Status is BookingStatus.PENDING or BookingStatus.CONFIRMED;

    // Chuyển đối tượng sang DTO.
    private static BookingDto ToDto(BookingEntity booking)
        => new(booking.Id, booking.CustomerId, booking.PetId, booking.BookingTime,
            booking.CheckIn, booking.CheckOut, booking.Note, booking.Status, booking.CreatedAt, booking.UpdatedAt,
            booking.BookingDetails.OrderBy(d => d.Id).Select(d => new BookingDetailDto(
                d.Id, d.ServiceOptionsId, d.UnitPrice, d.Status)).ToList());
}

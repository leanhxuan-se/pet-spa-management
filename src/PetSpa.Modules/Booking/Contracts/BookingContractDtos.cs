using PetSpa.Modules.Booking.Domain.Enums;

namespace PetSpa.Modules.Booking.Contracts;

public enum BookingContractStatus { Success, NotFound, Conflict }

public sealed record BookingContractResult<T>(BookingContractStatus Status, T? Value = default, string? Message = null)
{
    internal static BookingContractResult<T> Success(T value) => new(BookingContractStatus.Success, value);
    internal static BookingContractResult<T> NotFound() => new(BookingContractStatus.NotFound, Message: "Không tìm thấy lịch.");
    internal static BookingContractResult<T> Conflict(string message) => new(BookingContractStatus.Conflict, Message: message);
}

public sealed record BookingHandoffCandidate(long BookingId, long PetId, DateTime? BookingTime,
    IReadOnlyList<BookingSessionDetail> Details, DateTime? SessionsCreatedAt);

public sealed record BookingHandoff(long BookingId, DateTime SessionsCreatedAt, IReadOnlyList<long> BookingDetailIds);

public sealed record BookingSessionDetail(long BookingDetailId, long ServiceOptionId, BookingDetailStatus Status);

public sealed record BillableBooking(long BookingId, long CustomerId, BookingStatus Status,
    IReadOnlyList<BillableBookingDetail> Details);

public sealed record BillableBookingDetail(long BookingDetailId, long ServiceOptionId, decimal UnitPrice);

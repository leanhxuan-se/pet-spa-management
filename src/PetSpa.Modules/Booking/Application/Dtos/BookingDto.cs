// Định nghĩa dữ liệu trao đổi BookingDto.
using PetSpa.Modules.Booking.Domain.Enums;

namespace PetSpa.Modules.Booking.Application.Dtos;

public sealed record BookingDto(
    long Id, long CustomerId, long PetId, DateTime? BookingTime,
    DateTime? CheckIn, DateTime? CheckOut, string? Note, BookingStatus Status,
    DateTime CreatedAt, DateTime UpdatedAt, IReadOnlyList<BookingDetailDto> Services,
    string? ContactName, string? ContactPhone, DateTime? SessionsCreatedAt, DateTime? CancelledAt);

public sealed record BookingPageDto(IReadOnlyList<BookingDto> Items, string? NextCursor);

public sealed record BookingCursor(int StatusOrder, DateTime? BookingTime, long Id);

public sealed record BookingDetailDto(
    long Id, long ServiceOptionId, decimal UnitPrice, BookingDetailStatus Status);

public sealed record BookingActor(bool IsFrontDesk, long? CustomerId)
{
    public bool CanAccess(long customerId) => IsFrontDesk || CustomerId == customerId;
}

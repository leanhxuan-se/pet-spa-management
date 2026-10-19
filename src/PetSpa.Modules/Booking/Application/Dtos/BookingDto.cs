using PetSpa.Modules.Booking.Domain.Enums;

namespace PetSpa.Modules.Booking.Application.Dtos;

public sealed record BookingDto(
    long Id, long CustomerId, long PetId, DateTime BookingTime,
    DateTime? CheckIn, DateTime? CheckOut, string? Note, BookingStatus Status,
    DateTime CreatedAt, DateTime UpdatedAt, IReadOnlyList<BookingDetailDto> Services);

public sealed record BookingDetailDto(
    long Id, long ServiceOptionId, decimal UnitPrice, BookingDetailStatus Status);

public sealed record BookingActor(bool IsStaff, long? CustomerId)
{
    public bool CanAccess(long customerId) => IsStaff || CustomerId == customerId;
}

using System.Text.Json.Serialization;
using PetSpa.Modules.Booking.Domain.Enums;

namespace PetSpa.Modules.Booking.Application.Dtos;

public sealed record CreateBookingCommand(
    long CustomerId, long PetId, DateTime BookingTime, string? Note,
    IReadOnlyList<long>? ServiceOptionIds);

public sealed record UpdateBookingCommand(
    long CustomerId, long PetId, DateTime BookingTime, string? Note,
    [property: JsonRequired] BookingStatus Status, DateTime? CheckIn, DateTime? CheckOut);

public sealed record ReplaceBookingServicesCommand(IReadOnlyList<long>? ServiceOptionIds);

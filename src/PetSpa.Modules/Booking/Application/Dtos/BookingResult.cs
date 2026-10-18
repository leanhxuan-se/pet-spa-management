namespace PetSpa.Modules.Booking.Application.Dtos;

public enum BookingResultStatus { Success, NotFound, ValidationFailed, Conflict, Forbidden }

public sealed record BookingResult<T>(
    BookingResultStatus Status, T? Value = default, string? Message = null,
    IDictionary<string, string[]>? Errors = null)
{
    public static BookingResult<T> Success(T value) => new(BookingResultStatus.Success, value);
    public static BookingResult<T> NotFound() => new(BookingResultStatus.NotFound, Message: "Không tìm thấy booking.");
    public static BookingResult<T> Forbidden() => new(BookingResultStatus.Forbidden);
    public static BookingResult<T> Conflict(string message) => new(BookingResultStatus.Conflict, Message: message);
    public static BookingResult<T> Invalid(IDictionary<string, string[]> errors)
        => new(BookingResultStatus.ValidationFailed, Errors: errors);
}

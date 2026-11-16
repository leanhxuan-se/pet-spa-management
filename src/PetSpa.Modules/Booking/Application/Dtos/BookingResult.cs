// Định nghĩa dữ liệu trao đổi BookingResult.
namespace PetSpa.Modules.Booking.Application.Dtos;

public enum BookingResultStatus { Success, NotFound, ValidationFailed, Conflict, Forbidden }

public sealed record BookingResult<T>(
    BookingResultStatus Status, T? Value = default, string? Message = null,
    IDictionary<string, string[]>? Errors = null)
{
    // Tạo kết quả thành công.
    public static BookingResult<T> Success(T value) => new(BookingResultStatus.Success, value);
    // Tạo kết quả không tìm thấy.
    public static BookingResult<T> NotFound() => new(BookingResultStatus.NotFound, Message: "Không tìm thấy booking.");
    // Tạo kết quả từ chối truy cập.
    public static BookingResult<T> Forbidden() => new(BookingResultStatus.Forbidden);
    // Tạo kết quả báo xung đột.
    public static BookingResult<T> Conflict(string message) => new(BookingResultStatus.Conflict, Message: message);
    // Tạo kết quả báo dữ liệu không hợp lệ.
    public static BookingResult<T> Invalid(IDictionary<string, string[]> errors)
        => new(BookingResultStatus.ValidationFailed, Errors: errors);
}

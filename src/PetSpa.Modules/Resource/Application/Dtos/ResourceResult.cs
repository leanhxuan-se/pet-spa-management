// Định nghĩa dữ liệu trao đổi ResourceResult.
namespace PetSpa.Modules.Resource.Application.Dtos;

public enum ResourceResultStatus
{
    Success,
    NotFound,
    ValidationFailed,
    Conflict
}

public sealed record ResourceResult<T>(
    ResourceResultStatus Status,
    T? Value = default,
    string? Message = null,
    IDictionary<string, string[]>? Errors = null)
{
    // Tạo kết quả thành công.
    public static ResourceResult<T> Success(T value) => new(ResourceResultStatus.Success, value);
    // Tạo kết quả không tìm thấy.
    public static ResourceResult<T> NotFound(string message) => new(ResourceResultStatus.NotFound, Message: message);
    // Tạo kết quả báo xung đột.
    public static ResourceResult<T> Conflict(string message) => new(ResourceResultStatus.Conflict, Message: message);
    // Tạo kết quả báo dữ liệu không hợp lệ.
    public static ResourceResult<T> Invalid(IDictionary<string, string[]> errors)
        => new(ResourceResultStatus.ValidationFailed, Errors: errors);
}

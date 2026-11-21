// Định nghĩa hợp đồng hoặc kết quả dùng chung: Result.
namespace PetSpa.SharedKernel.Application.Abstractions
{

    public enum ResultStatus
    {
        Success,
        Created,
        NoContent,
        NotFound,
        ValidationFailed,
        TimeError,
        UnActive,
        BadRequest,
        Duplicate,
        Unauthorized,
        Forbidden,
        Conflict,
    }

    public class Result<T>
    {
        public ResultStatus Status { get; }
        public T? Value { get; }
        public IDictionary<string, string[]>? Errors { get; }

        // Tạo kết quả trả về.
        private Result(T? value, ResultStatus status)
        {
            Status = status;
            Value = value;
        }

        // Tạo kết quả trả về.
        private Result(IDictionary<string, string[]> errors, ResultStatus status)
        {
            Status = status;
            Errors = errors;
        }

        // Tạo kết quả thành công.
        public static Result<T> Success(T value) => new(value, ResultStatus.Success);

        public static Result<T> Created(T value) => new(value, ResultStatus.Created);

        public static Result<T> NoContent() => new(default(T), ResultStatus.NoContent);

        // Tạo kết quả không tìm thấy.
        public static Result<T> NotFound(string message) =>
            new(new Dictionary<string, string[]> { { "message", [message] } }, ResultStatus.NotFound);

        public static Result<T> TimeError(string message) =>
            new(new Dictionary<string, string[]> { { "message", [message] } }, ResultStatus.TimeError);

        // Tạo kết quả báo lỗi kiểm tra dữ liệu.
        public static Result<T> ValidationFailed(IDictionary<string, string[]> errors) =>
            new(errors, ResultStatus.ValidationFailed);

        // Tạo kết quả báo đối tượng không hoạt động.
        public static Result<T> UnActive(string key, string message) =>
            new(new Dictionary<string, string[]> { { key, [message] } }, ResultStatus.UnActive);

        public static Result<T> BadRequest(string key, string message) =>
            new(new Dictionary<string, string[]> { { key, [message] } }, ResultStatus.BadRequest);

        public static Result<T> Duplicate(string key, string message) =>
            new(new Dictionary<string, string[]> { { key, [message] } }, ResultStatus.Duplicate);

        // Tạo kết quả báo lỗi chưa xác thực (HTTP 401).
        public static Result<T> Unauthorized(string message) =>
            new(new Dictionary<string, string[]> { { "message", [message] } }, ResultStatus.Unauthorized);

        public static Result<T> Forbidden(string message) =>
            new(new Dictionary<string, string[]> { { "message", [message] } }, ResultStatus.Forbidden);

        public static Result<T> Forbidden(string key, string message) =>
            new(new Dictionary<string, string[]> { { key, [message] } }, ResultStatus.Forbidden);

        public static Result<T> Conflict(string message) =>
            new(new Dictionary<string, string[]> { { "message", [message] } }, ResultStatus.Conflict);

        public static Result<T> Conflict(string key, string message) =>
            new(new Dictionary<string, string[]> { { key, [message] } }, ResultStatus.Conflict);
        
    }
}

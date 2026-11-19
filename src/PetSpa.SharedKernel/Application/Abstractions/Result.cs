// Định nghĩa hợp đồng hoặc kết quả dùng chung: Result.




namespace PetSpa.SharedKernel.Application.Abstractions
{

    public enum ResultStatus
    {
        Success,
        Created,
        NotFound,
        ValidationFailed,
        TimeError,
        UnActive,
        BadRequest,
        Duplicate,
    }

    public class Result<T>
    {
        public ResultStatus Status { get; }
        public T? Value { get; }
        public IDictionary<string, string[]>? Errors { get; }

        // Tạo kết quả trả về.
        private Result(T value)
        {
            Status = ResultStatus.Success;
            Value = value;
        }

        // Tạo kết quả trả về.
        private Result(IDictionary<string, string[]> errors, ResultStatus status)
        {
            Status = status;
            Errors = errors;
        }

        // Tạo kết quả thành công.
        public static Result<T> Success(T value) => new(value);

        public static Result<T> Created(T value) => new(value);

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
        
    }
}

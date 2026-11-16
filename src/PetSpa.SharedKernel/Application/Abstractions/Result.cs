// Định nghĩa hợp đồng hoặc kết quả dùng chung: Result.




namespace PetSpa.SharedKernel.Application.Abstractions
{

    public enum ResultStatus
    {
        Success,
        NotFound,
        ValidationFailed,
        UnActive
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

        // Tạo kết quả không tìm thấy.
        public static Result<T> NotFound(string message) =>
            new(new Dictionary<string, string[]> { { "Customer", [message] } }, ResultStatus.NotFound);

        // Tạo kết quả báo lỗi kiểm tra dữ liệu.
        public static Result<T> ValidationFailed(IDictionary<string, string[]> errors) =>
            new(errors, ResultStatus.ValidationFailed);

        // Tạo kết quả báo đối tượng không hoạt động.
        public static Result<T> UnActive(string key, string message) =>
            new(new Dictionary<string, string[]> { { key, [message] } }, ResultStatus.UnActive);
        
    }
}





namespace PetSpa.SharedKernel.Application.Abstractions
{

    public enum ResultStatus
    {
        Success,
        NotFound,
        ValidationFailed
    }

    public class Result<T>
    {
        public ResultStatus Status { get; }
        public T? Value { get; }
        public IDictionary<string, string[]>? Errors { get; }

        private Result(T value)
        {
            Status = ResultStatus.Success;
            Value = value;
        }

        private Result(IDictionary<string, string[]> errors, ResultStatus status)
        {
            Status = status;
            Errors = errors;
        }

        public static Result<T> Success(T value) => new(value);

        public static Result<T> NotFound(string message) =>
            new(new Dictionary<string, string[]> { { "Customer", [message] } }, ResultStatus.NotFound);

        public static Result<T> ValidationFailed(IDictionary<string, string[]> errors) =>
            new(errors, ResultStatus.ValidationFailed);
    }
}

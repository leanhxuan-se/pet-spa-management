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
    public static ResourceResult<T> Success(T value) => new(ResourceResultStatus.Success, value);
    public static ResourceResult<T> NotFound(string message) => new(ResourceResultStatus.NotFound, Message: message);
    public static ResourceResult<T> Conflict(string message) => new(ResourceResultStatus.Conflict, Message: message);
    public static ResourceResult<T> Invalid(IDictionary<string, string[]> errors)
        => new(ResourceResultStatus.ValidationFailed, Errors: errors);
}

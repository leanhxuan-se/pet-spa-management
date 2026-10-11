namespace PetSpa.Modules.Operation.Application.Abstractions;

public enum PasswordCheckResult
{
    Failed,
    Success,
    SuccessRehashNeeded
}
namespace PetSpa.Modules.Customer.Application.Abstractions;

public enum PasswordCheckResult
{
    Failed,
    Success,
    SuccessRehashNeeded
}

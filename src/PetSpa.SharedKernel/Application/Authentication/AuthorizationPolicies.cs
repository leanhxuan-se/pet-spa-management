namespace PetSpa.SharedKernel.Application.Authentication;

public static class AuthorizationPolicies
{
    public const string AuthenticatedAccount = nameof(AuthenticatedAccount);
    public const string StaffOnly = nameof(StaffOnly);
    public const string CustomerManagement = nameof(CustomerManagement);
    public const string ManagerOrAdmin = nameof(ManagerOrAdmin);
}

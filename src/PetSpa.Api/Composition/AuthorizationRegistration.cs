using Microsoft.AspNetCore.Authorization;
using PetSpa.SharedKernel.Application.Authentication;

namespace PetSpa.Api.Composition;

public static class AuthorizationRegistration
{
    public static IServiceCollection AddAuthorizationServices(this IServiceCollection services)
    {
        return services.AddAuthorization(options =>
        {
            var authenticatedAccount = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .RequireAssertion(context => context.User.HasValidAccount())
                .Build();

            options.DefaultPolicy = authenticatedAccount;
            options.FallbackPolicy = authenticatedAccount;
            options.AddPolicy(AuthorizationPolicies.AuthenticatedAccount, authenticatedAccount);

            options.AddPolicy(AuthorizationPolicies.StaffOnly, policy =>
                policy.RequireAuthenticatedUser()
                    .RequireAssertion(context => context.User.HasStaffRole()));

            options.AddPolicy(AuthorizationPolicies.CustomerManagement, policy =>
                policy.RequireAuthenticatedUser()
                    .RequireAssertion(context => context.User.CanManageCustomers()));

            options.AddPolicy(AuthorizationPolicies.ManagerOrAdmin, policy =>
                policy.RequireAuthenticatedUser()
                    .RequireAssertion(context => context.User.HasStaffRole())
                    .RequireRole(AccountRoles.Manager, AccountRoles.Admin));
        });
    }
}

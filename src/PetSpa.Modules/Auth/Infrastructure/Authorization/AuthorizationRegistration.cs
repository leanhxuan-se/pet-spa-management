// Đăng ký và cấu hình thành phần AuthorizationRegistration.
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using PetSpa.SharedKernel.Application.Authentication;

namespace PetSpa.Modules.Auth.Infrastructure.Authorization;

internal static class AuthorizationRegistration
{
    // Đăng ký các chính sách phân quyền.
    public static IServiceCollection AddAuthPolicies(this IServiceCollection services)
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

            options.AddPolicy(AuthorizationPolicies.OperationAvailability, policy =>
                policy.RequireAuthenticatedUser()
                    .RequireAssertion(context => context.User.HasStaffRole())
                    .RequireRole(AccountRoles.Receptionist, AccountRoles.Manager));
        });
    }
}

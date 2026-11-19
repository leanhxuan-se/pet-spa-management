// Đăng ký và cấu hình thành phần AuthorizationRegistration.
using Microsoft.AspNetCore.Authorization;
using PetSpa.SharedKernel.Application.Authentication;

namespace PetSpa.Api.Composition;

public static class AuthorizationRegistration
{
    // Đăng ký các chính sách phân quyền.
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

            var frontDesk = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .RequireAssertion(context => context.User.HasFrontDeskRole())
                .Build();
            options.AddPolicy(AuthorizationPolicies.CustomerManagement, frontDesk);
            options.AddPolicy(AuthorizationPolicies.FrontDesk, frontDesk);

            options.AddPolicy(AuthorizationPolicies.ResourceManager, policy =>
                policy.RequireAuthenticatedUser()
                    .RequireAssertion(context => context.User.HasStaffRole())
                    .RequireRole(AccountRoles.Manager));

            options.AddPolicy(AuthorizationPolicies.ManagerOrAdmin, policy =>
                policy.RequireAuthenticatedUser()
                    .RequireAssertion(context => context.User.HasStaffRole())
                    .RequireRole(AccountRoles.Manager, AccountRoles.Admin));
        });
    }
}

using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PetSpa.SharedKernel.Application.Authentication;

namespace PetSpa.OperationSmokeTests;

internal sealed class OperationTestAuthentication(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("X-Test-Staff-Role", out var role))
            return Task.FromResult(AuthenticateResult.NoResult());

        var staffRole = role.ToString().ToLowerInvariant();
        var claims = new[]
        {
            new Claim(AuthClaimTypes.Subject, "staff:1"),
            new Claim(AuthClaimTypes.Role, staffRole),
            new Claim(AuthClaimTypes.AccountType, AccountTypes.Staff),
            new Claim(AuthClaimTypes.StaffId, "1")
        };
        var identity = new ClaimsIdentity(claims, Scheme.Name, AuthClaimTypes.Subject, AuthClaimTypes.Role);
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}

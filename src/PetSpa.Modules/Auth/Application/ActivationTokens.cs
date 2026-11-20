using Microsoft.AspNetCore.DataProtection;
using System.Security.Cryptography;

namespace PetSpa.Modules.Auth.Application;

internal sealed class ActivationTokens(IDataProtectionProvider provider)
{
    private readonly ITimeLimitedDataProtector protector = provider
        .CreateProtector("PetSpa.Auth.CustomerRegistration.v1").ToTimeLimitedDataProtector();

    public string Issue(long id, string phone) => protector.Protect(Payload(id, phone), TimeSpan.FromMinutes(15));

    public bool Matches(string token, long id, string phone)
    {
        try
        {
            return protector.Unprotect(token) == Payload(id, phone);
        }
        catch (Exception ex) when (ex is CryptographicException or FormatException)
        {
            return false;
        }
    }

    private static string Payload(long id, string phone) => FormattableString.Invariant($"{id}:{phone}");
}

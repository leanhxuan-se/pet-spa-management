using System.Text.RegularExpressions;

namespace PetSpa.Modules.Customer.Application;

internal static partial class CustomerContact
{
    public const int FullNameMaxLength = 150;
    public const int EmailMaxLength = 255;
    public const int PhoneMaxLength = 30;

    public static string NormalizePhone(string phone)
    {
        var trimmed = phone.Trim();
        if (trimmed.Length == 12 && trimmed.StartsWith("+84", StringComparison.Ordinal))
        {
            var local = "0" + trimmed[3..];
            if (LocalMobilePattern().IsMatch(local))
                return local;
        }

        return trimmed;
    }

    public static string? NormalizeEmail(string? email)
        => string.IsNullOrWhiteSpace(email) ? null : email.Trim().ToLowerInvariant();

    public static bool IsValidPhone(string? phone)
        => phone is not null && LocalMobilePattern().IsMatch(NormalizePhone(phone));

    [GeneratedRegex(@"\A0[35789][0-9]{8}\z", RegexOptions.CultureInvariant)]
    private static partial Regex LocalMobilePattern();
}

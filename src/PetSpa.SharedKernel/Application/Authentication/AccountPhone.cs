using System.Text.RegularExpressions;

namespace PetSpa.SharedKernel.Application.Authentication;

public static partial class AccountPhone
{
    public static string Normalize(string phone)
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

    public static bool IsVietnameseMobile(string phone) => LocalMobilePattern().IsMatch(Normalize(phone));

    [GeneratedRegex(@"\A0[35789][0-9]{8}\z", RegexOptions.CultureInvariant)]
    private static partial Regex LocalMobilePattern();
}

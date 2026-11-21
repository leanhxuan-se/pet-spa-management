using PetSpa.SharedKernel.Application.Authentication;

namespace PetSpa.Modules.Customer.Application;

internal static class CustomerContact
{
    public const int FullNameMaxLength = 150;
    public const int EmailMaxLength = 255;
    public const int PhoneMaxLength = 30;

    public static string NormalizePhone(string phone) => AccountPhone.Normalize(phone);

    public static string? NormalizeEmail(string? email)
        => string.IsNullOrWhiteSpace(email) ? null : email.Trim().ToLowerInvariant();

    public static bool IsValidPhone(string? phone)
        => phone is not null && AccountPhone.IsVietnameseMobile(phone);
}

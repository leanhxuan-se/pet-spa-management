using Microsoft.AspNetCore.Identity;
using PetSpa.SharedKernel.Application.Abstractions;

namespace PetSpa.Api.Authentication;

public sealed class IdentityPasswordService<TAccount>(
    IPasswordHasher<TAccount> passwordHasher) : IPasswordService<TAccount>
    where TAccount : class
{
    public string HashPassword(TAccount account, string password)
        => passwordHasher.HashPassword(account, password);

    public PasswordCheckResult VerifyHashedPassword(
        TAccount account,
        string passwordHash,
        string password)
        => passwordHasher.VerifyHashedPassword(account, passwordHash, password) switch
        {
            PasswordVerificationResult.Success => PasswordCheckResult.Success,
            PasswordVerificationResult.SuccessRehashNeeded => PasswordCheckResult.SuccessRehashNeeded,
            _ => PasswordCheckResult.Failed
        };
}

namespace PetSpa.SharedKernel.Application.Abstractions;

public interface IPasswordService<TAccount> where TAccount : class
{
    string HashPassword(TAccount account, string password);

    PasswordCheckResult VerifyHashedPassword(
        TAccount account,
        string passwordHash,
        string password);
}

using Microsoft.AspNetCore.Identity;
using PetSpa.Modules.Operation.Application.Abstractions;
using StaffTable = PetSpa.Modules.Operation.Domain.Entities.Staff;

namespace PetSpa.Modules.Operation.Infrastructure.Services;

public class IdentityPasswordService(
    IPasswordHasher<StaffTable> passwordHasher) : IPasswordService
{
    public string HashPassword(StaffTable staff, string password)
    {
        return passwordHasher.HashPassword(staff, password);
    }

    public PasswordCheckResult VerifyHashedPassword(
        StaffTable staff,
        string passwordHash,
        string password)
    {
        return passwordHasher.VerifyHashedPassword(staff, passwordHash, password) switch
        {
            PasswordVerificationResult.Success => PasswordCheckResult.Success,
            PasswordVerificationResult.SuccessRehashNeeded => PasswordCheckResult.SuccessRehashNeeded,
            _ => PasswordCheckResult.Failed
        };
    }
}

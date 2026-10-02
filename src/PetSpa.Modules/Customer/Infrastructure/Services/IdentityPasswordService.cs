using Microsoft.AspNetCore.Identity;
using PetSpa.Modules.Customer.Application.Abstractions;
using CustomerTable = PetSpa.Modules.Customer.Domain.Entities.Customer;

namespace PetSpa.Modules.Customer.Infrastructure.Services;

public class IdentityPasswordService(
    IPasswordHasher<CustomerTable> passwordHasher) : IPasswordService
{
    public string HashPassword(CustomerTable customer, string password)
    {
        return passwordHasher.HashPassword(customer, password);
    }

    public PasswordCheckResult VerifyHashedPassword(
        CustomerTable customer,
        string passwordHash,
        string password)
    {
        return passwordHasher.VerifyHashedPassword(customer, passwordHash, password) switch
        {
            PasswordVerificationResult.Success => PasswordCheckResult.Success,
            PasswordVerificationResult.SuccessRehashNeeded => PasswordCheckResult.SuccessRehashNeeded,
            _ => PasswordCheckResult.Failed
        };
    }
}

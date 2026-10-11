using CustomerTable = PetSpa.Modules.Customer.Domain.Entities.Customer;

namespace PetSpa.Modules.Customer.Application.Abstractions;

public interface IPasswordService
{
    string HashPassword(CustomerTable customer, string password);

    PasswordCheckResult VerifyHashedPassword(
        CustomerTable customer,
        string passwordHash,
        string password);
}

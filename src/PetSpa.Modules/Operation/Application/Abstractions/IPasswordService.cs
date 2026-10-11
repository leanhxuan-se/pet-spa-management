namespace PetSpa.Modules.Operation.Application.Abstractions;
using StaffTable = PetSpa.Modules.Operation.Domain.Entities.Staff;
public interface IPasswordService
{
    string HashPassword(StaffTable staff, string password);

    PasswordCheckResult VerifyHashedPassword(
        StaffTable customer,
        string passwordHash,
        string password);
}
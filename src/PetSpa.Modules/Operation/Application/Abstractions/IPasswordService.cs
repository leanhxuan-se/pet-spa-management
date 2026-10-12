namespace PetSpa.Modules.Operation.Application.Abstractions;
using StaffTable = PetSpa.Modules.Operation.Domain.Entities.Staff;
internal interface IPasswordService
{
    string HashPassword(StaffTable staff, string password);

    PasswordCheckResult VerifyHashedPassword(
        StaffTable staff,
        string passwordHash,
        string password);
}
namespace PetSpa.Modules.Operation.Application.Dtos;

public record StaffLoginResult(
    long Id,
    string FullName,
    string Phone,
    string Role
);

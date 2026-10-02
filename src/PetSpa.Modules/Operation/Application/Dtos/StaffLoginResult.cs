using PetSpa.Modules.Operation.Domain.Enums;
namespace PetSpa.Modules.Operation.Application.Dtos;

public record StaffLoginResult(
    long Id,
    string FullName,
    string Phone,
    StaffRole Role
);
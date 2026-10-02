namespace PetSpa.Modules.Customer.Application.Dtos;

public record CustomerLoginResult(
    long Id,
    string FullName,
    string Phone
);
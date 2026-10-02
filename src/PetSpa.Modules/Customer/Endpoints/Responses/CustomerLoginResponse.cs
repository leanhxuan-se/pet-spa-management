using PetSpa.Modules.Customer.Application.Dtos;

namespace PetSpa.Modules.Customer.Endpoints.Responses;

public record CustomerLoginResponse(
    string AccessToken,
    string TokenType,
    DateTime ExpiresAtUtc,
    CustomerLoginResult Customer
);
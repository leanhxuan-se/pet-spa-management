using PetSpa.Modules.Operation.Application.Dtos;

namespace PetSpa.Modules.Operation.Endpoints.Responses;

public record StaffLoginResponse(
    string AccessToken,
    string TokenType,
    DateTime ExpiresAtUtc,
    StaffLoginResult Staff
);
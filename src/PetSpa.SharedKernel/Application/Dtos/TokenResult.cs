namespace PetSpa.SharedKernel.Application.Dtos;

public record TokenResult(
    string AccessToken,
    DateTime ExpiresAtUtc
);
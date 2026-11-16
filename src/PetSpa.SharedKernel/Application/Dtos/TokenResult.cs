// Định nghĩa dữ liệu trao đổi TokenResult.
namespace PetSpa.SharedKernel.Application.Dtos;

public record TokenResult(
    string AccessToken,
    DateTime ExpiresAtUtc
);
// Định nghĩa dữ liệu trao đổi LoginResponse.
namespace PetSpa.Modules.Auth.Contracts;

public record LoginResponse(
    string AccessToken,
    string TokenType,
    DateTime ExpiresAtUtc,
    string Role,
    LoginUser User
);

public record LoginUser(long Id, string FullName, string Phone);

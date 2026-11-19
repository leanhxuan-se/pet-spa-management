// Định nghĩa cấu hình JwtOptions.
namespace PetSpa.Modules.Auth.Infrastructure.Configuration;

internal sealed class JwtOptions
{
    public string Issuer { get; set; } = "";
    public string Audience { get; set; } = "";
    public string SigningKey { get; set; } = "";
    public int AccessTokenMinutes { get; set; } = 15;
}

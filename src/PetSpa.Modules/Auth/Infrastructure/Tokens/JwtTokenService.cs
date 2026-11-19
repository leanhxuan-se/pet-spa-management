// Xử lý nghiệp vụ và cung cấp dịch vụ JwtTokenService.

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using PetSpa.Modules.Auth.Infrastructure.Configuration;

namespace PetSpa.Modules.Auth.Infrastructure.Tokens;

internal sealed class JwtTokenService(IOptions<JwtOptions> jwtOptions)
{
    private readonly JwtOptions options = jwtOptions.Value;

    // Tạo token từ các claim.
    public (string AccessToken, DateTime ExpiresAtUtc) Generate(IEnumerable<Claim> claims)
    {
        // 1. Xác định thời điểm tạo và hết hạn token.
        var now = DateTimeOffset.FromUnixTimeSeconds(DateTimeOffset.UtcNow.ToUnixTimeSeconds()).UtcDateTime;
        var expiresAt = now.AddMinutes(options.AccessTokenMinutes);

        // 2. Chuyển khóa cấu hình thành dạng thư viện có thể sử dụng.
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(options.SigningKey));

        // 3. Chọn khóa và thuật toán dùng để ký token.
        var credentials = new SigningCredentials(
            key,
            SecurityAlgorithms.HmacSha256);

        // 4. Bổ sung mã định danh riêng cho token.
        var tokenClaims = claims.Append(new Claim(
            JwtRegisteredClaimNames.Jti,
            Guid.NewGuid().ToString()));

        // 5. Tạo đối tượng JWT chứa thông tin và thời hạn.
        var token = new JwtSecurityToken(
            issuer: options.Issuer,
            audience: options.Audience,
            claims: tokenClaims,
            notBefore: now,
            expires: expiresAt,
            signingCredentials: credentials);

        // 6. Chuyển đối tượng JWT thành chuỗi gửi được cho client.
        var accessToken = new JwtSecurityTokenHandler()
            .WriteToken(token);

        return (accessToken, expiresAt);
    }
}

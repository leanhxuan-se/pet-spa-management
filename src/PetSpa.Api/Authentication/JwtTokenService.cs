
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using PetSpa.Api.Configuration;
using PetSpa.SharedKernel.Application.Abstractions;
using PetSpa.SharedKernel.Application.Dtos;

namespace PetSpa.Api.Authentication;

public class JwtTokenService(
    IOptions<JwtOptions> jwtOptions) : ITokenService
{
    private readonly JwtOptions options = jwtOptions.Value;

    public TokenResult Generate(IEnumerable<Claim> claims)
    {
        var now = DateTime.UtcNow;
        var expiresAt = now.AddMinutes(options.AccessTokenMinutes);

        // Chuyển khóa cấu hình thành dạng thư viện có thể sử dụng.
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(options.SigningKey));

        // Chọn khóa và thuật toán dùng để ký token.
        var credentials = new SigningCredentials(
            key,
            SecurityAlgorithms.HmacSha256);

        // Bổ sung mã định danh riêng cho token.
        var tokenClaims = claims.Append(new Claim(
            JwtRegisteredClaimNames.Jti,
            Guid.NewGuid().ToString()));

        // Tạo đối tượng JWT chứa thông tin và thời hạn.
        var token = new JwtSecurityToken(
            issuer: options.Issuer,
            audience: options.Audience,
            claims: tokenClaims,
            notBefore: now,
            expires: expiresAt,
            signingCredentials: credentials);

        // Chuyển đối tượng JWT thành chuỗi gửi được cho client.
        var accessToken = new JwtSecurityTokenHandler()
            .WriteToken(token);

        return new TokenResult(accessToken, expiresAt);
    }
}
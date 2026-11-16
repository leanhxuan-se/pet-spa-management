// Đăng ký và cấu hình thành phần AuthenticationRegistration.
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PetSpa.Api.Authentication;
using PetSpa.Api.Configuration;
using PetSpa.SharedKernel.Application.Abstractions;
using PetSpa.SharedKernel.Application.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;



namespace PetSpa.Api.Composition;

public static class AuthenticationRegistration
{
    // Đăng ký dịch vụ xác thực và cấu hình JWT.
    public static IServiceCollection AddAuthenticationServices(this IServiceCollection services, IConfiguration configuration)
    {
        // 1. Đọc và kiểm tra cấu hình JWT.
        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection("Jwt"))
            .Validate(
                options =>
                    !string.IsNullOrWhiteSpace(options.Issuer) &&
                    !string.IsNullOrWhiteSpace(options.Audience),
                "Jwt:Issuer và Jwt:Audience không được để trống.")
            .Validate(
                options => options.AccessTokenMinutes > 0,
                "Thời hạn token phải lớn hơn 0 phút.")
            .Validate(
                options =>
                    !string.IsNullOrWhiteSpace(options.SigningKey) &&
                    Encoding.UTF8.GetByteCount(options.SigningKey) >= 32,
                "Jwt:SigningKey phải có ít nhất 32 byte khi đọc bằng UTF-8.")
            .ValidateOnStart();

        // 2. Đăng ký các dịch vụ cần sử dụng.
        services.AddScoped(typeof(IPasswordHasher<>), typeof(PasswordHasher<>));
        services.AddScoped(typeof(IPasswordService<>), typeof(IdentityPasswordService<>));
        services.AddSingleton<ITokenService, JwtTokenService>();
        // 3. Đăng ký xác thực bằng JWT.
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();

        // 4. Cấu hình kiểm tra token khi nhận yêu cầu.
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
        .Configure<IOptions<JwtOptions>>((bearerOptions, jwtOptions) =>
    {
        var jwt = jwtOptions.Value;

        bearerOptions.MapInboundClaims = false;

        bearerOptions.TokenValidationParameters =
            new TokenValidationParameters
            {
                NameClaimType = AuthClaimTypes.Subject,
                RoleClaimType = AuthClaimTypes.Role,

                ValidateIssuer = true,
                ValidIssuer = jwt.Issuer,

                ValidateAudience = true,
                ValidAudience = jwt.Audience,

                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(jwt.SigningKey)),

                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromSeconds(30)
            };
    });

        return services;
    }
}

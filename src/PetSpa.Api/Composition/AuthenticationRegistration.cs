using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PetSpa.Api.Authentication;
using PetSpa.Api.Configuration;
using PetSpa.SharedKernel.Application.Abstractions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;



namespace PetSpa.Api.Composition;

public static class AuthenticationRegistration
{
    public static IServiceCollection AddAuthenticationServices(this IServiceCollection services, IConfiguration configuration)
    {
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

        services.AddSingleton<ITokenService, JwtTokenService>();
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();

        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
        .Configure<IOptions<JwtOptions>>((bearerOptions, jwtOptions) =>
    {
        var jwt = jwtOptions.Value;

        bearerOptions.MapInboundClaims = false;

        bearerOptions.TokenValidationParameters =
            new TokenValidationParameters
            {
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

        services.AddAuthorization();
        return services;
    }
}
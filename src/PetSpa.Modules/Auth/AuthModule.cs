// Đăng ký dịch vụ và endpoint của module Auth.
using System.Text;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using PetSpa.Modules.Auth.Application;
using PetSpa.Modules.Auth.Endpoints;
using PetSpa.Modules.Auth.Infrastructure.Authorization;
using PetSpa.Modules.Auth.Infrastructure.Configuration;
using PetSpa.Modules.Auth.Infrastructure.Identity;
using PetSpa.Modules.Auth.Infrastructure.Persistence;
using PetSpa.Modules.Auth.Infrastructure.Tokens;
using PetSpa.SharedKernel.Application.Abstractions;
using PetSpa.SharedKernel.Application.Authentication;

namespace PetSpa.Modules.Auth;

public static class AuthModule
{
    // Đăng ký cấu hình, dịch vụ xác thực và chính sách phân quyền.
    public static IServiceCollection AddAuthModule(
        this IServiceCollection services, IConfiguration configuration, string connectionString)
    {
        services.AddDbContext<AuthDbContext>(options => options.UseNpgsql(connectionString, postgres =>
        {
            postgres.MigrationsAssembly(typeof(AuthDbContext).Assembly.GetName().Name);
            postgres.MigrationsHistoryTable("__EFMigrationsHistory", AuthDbContext.Schema);
        }));
        services.AddIdentityCore<AppUser>(options => options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15))
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<AuthDbContext>()
            .AddDefaultTokenProviders();

        // 1. Kiểm tra cấu hình JWT khi ứng dụng khởi động.
        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection("Jwt"))
            .Validate(options => !string.IsNullOrWhiteSpace(options.Issuer) &&
                !string.IsNullOrWhiteSpace(options.Audience), "Jwt:Issuer và Jwt:Audience không được để trống.")
            .Validate(options => options.AccessTokenMinutes > 0, "Thời hạn token phải lớn hơn 0 phút.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.SigningKey) &&
                Encoding.UTF8.GetByteCount(options.SigningKey) >= 32,
                "Jwt:SigningKey phải có ít nhất 32 byte khi đọc bằng UTF-8.")
            .ValidateOnStart();

        // 2. Đăng ký dịch vụ đăng nhập, băm mật khẩu và phát token.
        services.AddScoped(typeof(IPasswordHasher<>), typeof(PasswordHasher<>));
        services.AddScoped(typeof(IPasswordService<>), typeof(IdentityPasswordService<>));
        services.AddScoped<IValidator<LoginRequest>, LoginRequestValidator>();
        services.AddScoped<LoginService>();
        services.AddSingleton<JwtTokenService>();

        // 3. Cấu hình xác thực JWT và các chính sách phân quyền.
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>>((bearerOptions, jwtOptions) =>
            {
                var jwt = jwtOptions.Value;
                bearerOptions.MapInboundClaims = false;
                bearerOptions.TokenValidationParameters = new TokenValidationParameters
                {
                    NameClaimType = AuthClaimTypes.Subject,
                    RoleClaimType = AuthClaimTypes.Role,
                    ValidateIssuer = true,
                    ValidIssuer = jwt.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwt.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30)
                };
            });

        return services.AddAuthPolicies();
    }

    // Đăng ký các endpoint đăng nhập của module.
    public static IEndpointRouteBuilder MapAuthModuleEndpoint(this IEndpointRouteBuilder app)
        => app.MapAuthEndpoints();
}

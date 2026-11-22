using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using PetSpa.Modules.Auth.Application.Services;
using PetSpa.Modules.Auth.Endpoints;
using PetSpa.Modules.Auth.Infrastructure.Configuration;
using PetSpa.Modules.Auth.Infrastructure.Persistence;
using PetSpa.Modules.Auth.Infrastructure.Tokens;
using PetSpa.SharedKernel.Application.Authentication;

namespace PetSpa.Modules.Auth;

public static class AuthModule
{
    public static IServiceCollection AddAuthModule(
        this IServiceCollection services, IConfiguration configuration, string connectionString)
    {
        services.AddDbContext<AuthDbContext>(options => options.UseNpgsql(connectionString, postgres =>
            postgres.MigrationsHistoryTable("__EFMigrationsHistory", AuthDbContext.Schema)));
        services.AddScoped<IPasswordHasher<Account>, PasswordHasher<Account>>();
        services.AddScoped<LoginService>();
        services.AddScoped<AccountService>();
        services.AddScoped<IStaffAccountGuard>(sp => sp.GetRequiredService<AccountService>());
        services.AddSingleton<JwtTokenService>();
        services.AddSingleton<ActivationTokens>();
        services.AddDataProtection().SetApplicationName("PetSpa");

        services.AddOptions<JwtOptions>().Bind(configuration.GetSection("Jwt"))
            .Validate(o => !string.IsNullOrWhiteSpace(o.Issuer) && !string.IsNullOrWhiteSpace(o.Audience),
                "Jwt:Issuer and Jwt:Audience are required.")
            .Validate(o => o.AccessTokenMinutes > 0, "Jwt:AccessTokenMinutes must be positive.")
            .Validate(o => !string.IsNullOrWhiteSpace(o.SigningKey) && Encoding.UTF8.GetByteCount(o.SigningKey) >= 32,
                "Jwt:SigningKey must contain at least 32 bytes.")
            .ValidateOnStart();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>>((bearer, configured) =>
            {
                var jwt = configured.Value;
                bearer.MapInboundClaims = false;
                bearer.TokenValidationParameters = new TokenValidationParameters
                {
                    NameClaimType = AuthClaimTypes.Subject,
                    RoleClaimType = AuthClaimTypes.Role,
                    ValidateIssuer = true,
                    ValidIssuer = jwt.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwt.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
                    ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30)
                };

                bearer.Events = new JwtBearerEvents
                {
                    OnTokenValidated = async context =>
                    {
                        var login = context.HttpContext.RequestServices.GetRequiredService<LoginService>();
                        try
                        {
                            if (context.Principal is null ||
                                !await login.ValidateAsync(context.Principal, context.HttpContext.RequestAborted))
                                context.Fail("Account or session is invalid.");
                        }
                        catch (NpgsqlException)
                        {
                            context.HttpContext.Items[typeof(NpgsqlException)] = true;
                            context.Fail("Account validation is temporarily unavailable.");
                        }
                    },
                    OnChallenge = context =>
                    {
                        if (context.HttpContext.Items.ContainsKey(typeof(NpgsqlException)))
                        {
                            context.HandleResponse();
                            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
                        }
                        return Task.CompletedTask;
                    }
                };
            });

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy("auth-login", context => LimitByIp(context, 30));
            options.AddPolicy("auth-register", context => LimitByIp(context, 10));
        });

        return services.AddAuthPolicies();
    }

    private static RateLimitPartition<string> LimitByIp(HttpContext context, int limit) =>
        RateLimitPartition.GetFixedWindowLimiter(context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions { PermitLimit = limit, Window = TimeSpan.FromMinutes(1) });

    public static IEndpointRouteBuilder MapAuthModuleEndpoint(this IEndpointRouteBuilder app) => app.MapAuthEndpoints();
}

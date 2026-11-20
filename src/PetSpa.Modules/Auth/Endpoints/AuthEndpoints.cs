using System.Security.Claims;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using PetSpa.Modules.Auth.Application;
using PetSpa.SharedKernel.Application.Authentication;
using PetSpa.SharedKernel.Application.Dtos;

namespace PetSpa.Modules.Auth.Endpoints;

internal static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth").WithTags("Auth");
        group.AddEndpointFilter(async (context, next) =>
        {
            try
            {
                return await next(context);
            }
            catch (NpgsqlException)
            {
                return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
            }
            catch (DbUpdateException ex) when (ex.InnerException is NpgsqlException)
            {
                return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
            }
        });

        group.MapPost("/register", RegisterAsync).WithName("Register").AllowAnonymous()
            .RequireRateLimiting("auth-register").Produces<CustomerAccountResponse>(201).Produces(400).Produces(409).Produces(429);
        group.MapPost("/login", LoginAsync).WithName("Login").AllowAnonymous()
            .RequireRateLimiting("auth-login").Produces<LoginResponse>().Produces(400).Produces(401).Produces(429);
        group.MapPost("/logout", LogoutAsync).WithName("Logout").RequireAuthorization().Produces(204).Produces(401);

        group.MapPost("/customers/{customerId:long}/activation", IssueActivationAsync).WithName("IssueCustomerActivation")
            .RequireAuthorization(AuthorizationPolicies.CustomerManagement).Produces<ActivationResponse>().Produces(404);
        group.MapPost("/staff-accounts", CreateStaffAsync).WithName("CreateStaffAccount")
            .RequireAuthorization(AuthorizationPolicies.ManagerOrAdmin).Produces<StaffAccountResponse>(201)
            .Produces(400).Produces(404).Produces(409);
        group.MapPatch("/staff-accounts/{staffId:long}", UpdateStaffAsync).WithName("UpdateStaffAccount")
            .RequireAuthorization(AuthorizationPolicies.ManagerOrAdmin).Produces(204).Produces(400).Produces(404).Produces(409);
        return app;
    }

    private static async Task<IResult> RegisterAsync(RegisterRequest request, AccountService accounts, CancellationToken ct)
    {
        if (request.Phone is null || request.Phone.Length > 30 || !AccountPhone.IsVietnameseMobile(request.Phone) ||
            !ValidNewPassword(request.Password) || request.ActivationToken is not { Length: > 0 and <= 1024 })
            return InvalidRegistration();

        var result = await accounts.RegisterAsync(AccountPhone.Normalize(request.Phone), request.Password!, request.ActivationToken, ct);
        return result.Status switch
        {
            AccountChangeStatus.Success => Results.Created((string?)null, new CustomerAccountResponse(result.ProfileId!.Value)),
            AccountChangeStatus.Conflict => Results.Conflict(),
            _ => InvalidRegistration()
        };
    }

    private static async Task<IResult> LoginAsync(LoginRequest request, HttpContext context, LoginService login, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Phone) || request.Phone.Length > 30 ||
            string.IsNullOrEmpty(request.Password) || request.Password.Length > 4096)
            return Results.BadRequest(new { message = "Cần số điện thoại và mật khẩu hợp lệ." });

        var response = await login.LoginAsync(AccountPhone.Normalize(request.Phone), request.Password, ct);
        context.Response.Headers.CacheControl = "no-store";
        return response is null ? Results.Unauthorized() : Results.Ok(response);
    }

    private static async Task<IResult> LogoutAsync(ClaimsPrincipal user, LoginService login, CancellationToken ct) =>
        await login.LogoutAsync(user, ct) ? Results.NoContent() : Results.Unauthorized();

    private static async Task<IResult> IssueActivationAsync(long customerId, HttpContext context, AccountService accounts, CancellationToken ct)
    {
        var token = await accounts.IssueActivationAsync(customerId, ct);
        if (token is null)
            return Results.NotFound();
        context.Response.Headers.CacheControl = "no-store";
        return Results.Ok(new ActivationResponse(token));
    }

    private static async Task<IResult> CreateStaffAsync(
        StaffAccountRequest request, ClaimsPrincipal actor, AccountService accounts, CancellationToken ct)
    {
        if (request.StaffId <= 0 || !ValidNewPassword(request.Password))
            return Results.BadRequest();
        var result = await accounts.CreateStaffAsync(request.StaffId, request.Password!, actor, ct);
        return result.Status == AccountChangeStatus.Success
            ? Results.Created((string?)null, new StaffAccountResponse(request.StaffId))
            : StaffChangeResult(result.Status);
    }

    private static async Task<IResult> UpdateStaffAsync(long staffId, UpdateStaffAccountRequest request,
        ClaimsPrincipal actor, AccountService accounts, CancellationToken ct)
    {
        if (staffId <= 0 || (request.Password is null && !request.IsEnabled.HasValue) ||
            (request.Password is not null && !ValidNewPassword(request.Password)))
            return Results.BadRequest();

        return StaffChangeResult(await accounts.UpdateStaffAsync(staffId, request.Password, request.IsEnabled, actor, ct));
    }

    private static IResult StaffChangeResult(AccountChangeStatus status) => status switch
    {
        AccountChangeStatus.Success => Results.NoContent(),
        AccountChangeStatus.Missing => Results.NotFound(),
        AccountChangeStatus.Forbidden => Results.Forbid(),
        AccountChangeStatus.Conflict => Results.Conflict(),
        _ => Results.BadRequest()
    };

    private static bool ValidNewPassword(string? value) => value is { Length: >= 12 and <= 128 } && !string.IsNullOrWhiteSpace(value);

    private static IResult InvalidRegistration() =>
        Results.BadRequest(new { message = "Không thể kích hoạt tài khoản với thông tin đã cung cấp." });

    [JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
    private sealed record RegisterRequest(string? Phone, string? Password, string? ActivationToken);

    [JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
    private sealed record StaffAccountRequest(long StaffId, string? Password);

    [JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
    private sealed record UpdateStaffAccountRequest(string? Password, bool? IsEnabled);

    private sealed record ActivationResponse(string ActivationToken);
    private sealed record CustomerAccountResponse(long CustomerId);
    private sealed record StaffAccountResponse(long StaffId);
}

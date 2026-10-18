using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PetSpa.Modules.Operation;
using PetSpa.Modules.Operation.Application.Abstractions;
using PetSpa.Modules.Operation.Application.Services;
using PetSpa.Modules.Operation.Domain.Repositories;
using PetSpa.OperationSmokeTests;
using PetSpa.SharedKernel.Application.Abstractions;
using PetSpa.SharedKernel.Application.Dtos;

const string Connection = "Host=localhost;Database=unused_operation_tests;Username=unused";
const string StaffPassword = "TestPassword123!";
var checks = 0;
void Check(bool condition, string message)
{
    if (!condition)
        throw new InvalidOperationException(message);
    checks++;
}

var builder = WebApplication.CreateBuilder();
builder.WebHost.UseUrls("http://127.0.0.1:0");
builder.Services.AddOperationModule(Connection);
builder.Services.AddSingleton<OperationTestStore>();
builder.Services.AddScoped<IRoomRepository, FakeRoomRepository>();
builder.Services.AddScoped<IStaffRepository, FakeStaffRepository>();
builder.Services.AddScoped<IStaffAuthRepository, FakeStaffAuthRepository>();
builder.Services.AddSingleton<ITokenService, TestTokenService>();
builder.Services.AddAuthentication("OperationStaffTest")
    .AddScheme<AuthenticationSchemeOptions, OperationTestAuthentication>("OperationStaffTest", _ => { });
builder.Services.AddAuthorization();

await using var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();
app.MapOperationModuleEndpoint();

using (var scope = app.Services.CreateScope())
{
    var store = scope.ServiceProvider.GetRequiredService<OperationTestStore>();
    var passwordService = scope.ServiceProvider.GetRequiredService<IPasswordService>();
    var manager = store.StaffMembers.Single(staff => staff.Id == 1);
    manager.PasswordHash = passwordService.HashPassword(manager, StaffPassword);
}

await app.StartAsync();
var address = app.Services.GetRequiredService<IServer>().Features
    .Get<IServerAddressesFeature>()!.Addresses.Single();
using var client = new HttpClient { BaseAddress = new Uri(address) };

async Task<JsonElement> SendAsync(
    HttpMethod method,
    string path,
    HttpStatusCode expected,
    object? body = null,
    string? staffRole = "MANAGER")
{
    using var request = new HttpRequestMessage(method, path);
    if (body is not null)
        request.Content = JsonContent.Create(body);
    if (staffRole is not null)
        request.Headers.Add("X-Test-Staff-Role", staffRole);

    using var response = await client.SendAsync(request);
    var content = await response.Content.ReadAsStringAsync();
    Check(response.StatusCode == expected,
        $"{method} {path}: expected {expected}, got {response.StatusCode}: {content}");
    return string.IsNullOrEmpty(content)
        ? default
        : JsonDocument.Parse(content).RootElement.Clone();
}

const string ValidRange = "startTime=2027-05-01T12%3A00%3A00&endTime=2027-05-01T13%3A00%3A00";
const string InvalidRange = "startTime=2027-05-01T12%3A00%3A00&endTime=2027-05-01T12%3A00%3A00";

await SendAsync(HttpMethod.Get, $"/api/room/availability?{ValidRange}", HttpStatusCode.Unauthorized, staffRole: null);
var availableRooms = await SendAsync(
    HttpMethod.Get, $"/api/room/availability?{ValidRange}", HttpStatusCode.OK);
Check(availableRooms.GetArrayLength() == 1, "Room availability should return one available room");
Check(availableRooms[0].GetProperty("roomName").GetString() == "Bath Room", "Available room name");

var unavailableRooms = await SendAsync(
    HttpMethod.Get, $"/api/room/unavailability?{ValidRange}", HttpStatusCode.OK);
Check(unavailableRooms.GetArrayLength() == 1, "Room unavailability should return one unavailable room");
Check(unavailableRooms[0].GetProperty("roomName").GetString() == "Grooming Room", "Unavailable room name");
await SendAsync(HttpMethod.Get, $"/api/room/availability?{InvalidRange}", HttpStatusCode.BadRequest);

var availableStaff = await SendAsync(
    HttpMethod.Get, $"/api/staff/availability?{ValidRange}", HttpStatusCode.OK);
Check(availableStaff.GetArrayLength() == 2, "Staff availability should return two available staff members");

var unavailableStaff = await SendAsync(
    HttpMethod.Get, $"/api/staff/unavailability?{ValidRange}", HttpStatusCode.OK);
Check(unavailableStaff.GetArrayLength() == 1, "Staff unavailability should return one unavailable staff member");

var login = await SendAsync(
    HttpMethod.Post,
    "/api/staff/login",
    HttpStatusCode.OK,
    new { phone = " 0900000001 ", password = StaffPassword },
    staffRole: null);
Check(login.GetProperty("accessToken").GetString() == "operation-test-token", "Staff login should return a token");
Check(login.GetProperty("staff").GetProperty("id").GetInt64() == 1, "Staff login should return the seeded staff record");
Check(app.Services.GetRequiredService<OperationTestStore>().StaffSaveCount == 1, "Staff login should save the login timestamp");
await SendAsync(
    HttpMethod.Post,
    "/api/staff/login",
    HttpStatusCode.Unauthorized,
    new { phone = "0900000001", password = "invalid" },
    staffRole: null);

Console.WriteLine($"Operation module smoke tests passed ({checks} checks).");

internal sealed class TestTokenService : ITokenService
{
    public TokenResult Generate(IEnumerable<Claim> claims)
    {
        var tokenClaims = claims.ToArray();
        if (!tokenClaims.Any(claim => claim.Type == "account_type" && claim.Value == "staff"))
            throw new InvalidOperationException("Operation login must generate staff authentication claims.");
        return new TokenResult("operation-test-token", DateTime.UtcNow.AddMinutes(5));
    }
}

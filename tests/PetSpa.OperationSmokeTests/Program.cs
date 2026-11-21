using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PetSpa.Modules.Operation;
using PetSpa.Modules.Auth;
using PetSpa.Modules.Billing;
using PetSpa.Modules.Booking;
using PetSpa.Modules.Operation.Application.Abstractions;
using PetSpa.Modules.Operation.Application.Services;
using PetSpa.OperationSmokeTests;

const string Connection = "Host=localhost;Database=unused_operation_tests;Username=unused";
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
builder.Services.AddBookingModule(Connection);
builder.Services.AddBillingModule(Connection);
builder.Services.AddSingleton<OperationTestStore>();
builder.Services.AddScoped<IRoomRepository, FakeRoomRepository>();
builder.Services.AddScoped<IStaffRepository, FakeStaffRepository>();
builder.Services.AddAuthentication("OperationStaffTest")
    .AddScheme<AuthenticationSchemeOptions, OperationTestAuthentication>("OperationStaffTest", _ => { });
builder.Services.AddAuthPolicies();

await using var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();
app.MapOperationModuleEndpoint();

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

await SendAsync(HttpMethod.Get, $"/api/room/available?{ValidRange}", HttpStatusCode.Unauthorized, staffRole: null);
var availableRooms = await SendAsync(
    HttpMethod.Get, $"/api/room/available?{ValidRange}", HttpStatusCode.OK);
Check(availableRooms.GetArrayLength() == 1, "Room availability should return one available room");
Check(availableRooms[0].GetProperty("roomName").GetString() == "Bath Room", "Available room name");

var unavailableRooms = await SendAsync(
    HttpMethod.Get, $"/api/room/unavailable?{ValidRange}", HttpStatusCode.OK);
Check(unavailableRooms.GetArrayLength() == 1, "Room unavailability should return one unavailable room");
Check(unavailableRooms[0].GetProperty("roomName").GetString() == "Grooming Room", "Unavailable room name");
await SendAsync(HttpMethod.Get, $"/api/room/available?{InvalidRange}", HttpStatusCode.BadRequest);

var availableStaff = await SendAsync(
    HttpMethod.Get, $"/api/staff/available?{ValidRange}", HttpStatusCode.OK, staffRole: "RECEPTIONIST");
Check(availableStaff.GetArrayLength() == 2, "Staff availability should return two available staff members");

var unavailableStaff = await SendAsync(
    HttpMethod.Get, $"/api/staff/unavailable?{ValidRange}", HttpStatusCode.OK);
Check(unavailableStaff.GetArrayLength() == 1, "Staff unavailability should return one unavailable staff member");
await SendAsync(HttpMethod.Get, "/api/staff/available", HttpStatusCode.OK);
await SendAsync(HttpMethod.Get, "/api/staff/unavailable", HttpStatusCode.Forbidden, staffRole: "STAFF");
await SendAsync(HttpMethod.Get, $"/api/staff/available?{InvalidRange}", HttpStatusCode.BadRequest);
await SendAsync(HttpMethod.Get, "/api/staff/available?startTime=2027-05-01T12:00:00Z", HttpStatusCode.BadRequest);
await SendAsync(HttpMethod.Get, "/api/staff/availability", HttpStatusCode.NotFound);

await SendAsync(HttpMethod.Post, "/api/auth/staff/login", HttpStatusCode.NotFound);

Console.WriteLine($"Operation module smoke tests passed ({checks} checks).");

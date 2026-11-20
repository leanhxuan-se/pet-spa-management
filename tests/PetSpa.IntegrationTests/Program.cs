using PetSpa.Modules.Customer.Domain.Enums;
using Npgsql;
using PetSpa.Modules.Auth;
using PetSpa.Modules.Customer;
using PetSpa.Modules.Operation;
// Kiểm thử các chức năng và quyền truy cập của API.
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.IdentityModel.Tokens;
using PetSpa.Api.Composition;
using PetSpa.Modules.Booking.Application.Abstractions;
using PetSpa.Modules.Booking.Application.Dtos;
using PetSpa.Modules.Booking.Application.Services;
using PetSpa.Modules.Booking.Domain.Entities;
using PetSpa.Modules.Booking.Endpoints;
using PetSpa.Modules.Customer.Application.Abstractions;
using PetSpa.Modules.Customer.Contracts;
using PetSpa.Modules.Customer.Contracts.Dtos;
using PetSpa.Modules.Customer.Endpoints;
using PetSpa.Modules.Customer.Endpoints.Requests;
using PetSpa.Modules.Customer.Endpoints.Responses;
using PetSpa.Modules.Operation.Domain.Entities;
using PetSpa.Modules.Operation.Domain.Enums;
using PetSpa.Modules.Operation.Endpoints;
using PetSpa.Modules.Operation.Infrastructure.Persistence;
using PetSpa.Modules.Resource.Application.Abstractions;
using PetSpa.Modules.Resource.Application.Services;
using PetSpa.Modules.Resource.Contracts;
using PetSpa.Modules.Resource.Domain.Entities;
using PetSpa.Modules.Resource.Endpoints;
using PetSpa.SharedKernel.Application.Abstractions;
using PetSpa.SharedKernel.Application.Authentication;
using PetStatus = PetSpa.Modules.Customer.Domain.Enums.PetStatus;
using Species = PetSpa.Modules.Customer.Domain.Enums.Species;

const string key = "smoke-tests-only-synthetic-signing-key-0123456789";
const string password = "Smoke-tests-only-passphrase!123";
var connection = Environment.GetEnvironmentVariable("PETSPA_AUTH_TEST_CONNECTION");
if (string.IsNullOrWhiteSpace(connection))
{
    Console.Error.WriteLine("Set PETSPA_AUTH_TEST_CONNECTION to a fresh disposable PostgreSQL database.");
    return 2;
}

const string customerPhone = "0900000001";
const string staffPhone = "0900000002";
const string unregisteredPhone = "0900000003";
const string adminPhone = "0900000004";
var legacyHasher = new PasswordHasher<object>(Microsoft.Extensions.Options.Options.Create(
    new PasswordHasherOptions { CompatibilityMode = PasswordHasherCompatibilityMode.IdentityV2 }));
var legacyHash = legacyHasher.HashPassword(new object(), password);
var pets = new FakePets();

var bookings = new FakeBookings();
var categoryRepository = new FakeCategories();
var customerService = new FakeCustomerService();
var builder = WebApplication.CreateBuilder(new WebApplicationOptions { Args = [], EnvironmentName = "Production" });
builder.Configuration.Sources.Clear();
builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
{
    ["Jwt:Issuer"] = "role-tests", ["Jwt:Audience"] = "role-tests-client", ["Jwt:SigningKey"] = key
});
builder.Logging.ClearProviders();
builder.WebHost.UseUrls("http://127.0.0.1:0");
builder.Services.AddAuthModule(builder.Configuration, connection);
builder.Services.AddDataProtection().UseEphemeralDataProtectionProvider();
builder.Services.AddCustomerModule(connection);
builder.Services.AddOperationModule(connection);
builder.Services.AddPetSpaOpenApi();
builder.Services.ConfigureHttpJsonOptions(options => options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddSingleton<IPetServices>(pets);
builder.Services.AddSingleton<ICustomerService>(customerService);
builder.Services.AddSingleton<IBookingRepository>(bookings);
builder.Services.AddSingleton<ICategoryRepository>(categoryRepository);
builder.Services.AddSingleton<ICustomerModule, FakeCustomerModule>();
builder.Services.AddSingleton<IResourceModule, FakeResourceModule>();
builder.Services.AddScoped<BookingService>();
builder.Services.AddScoped<CategoryService>();
await using var app = builder.Build();
app.UseSwaggerUI();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
app.MapAuthModuleEndpoint();
app.MapCustomerEndpoint();
app.MapPetEndpoint();
app.MapBookingEndpoints();
app.MapCategoryEndpoints();
app.MapHealthEndpoint();
app.MapOpenApi().AllowAnonymous();
app.MapOpenApi("/swagger/{documentName}/swagger.json").AllowAnonymous();
app.MapGet("/test/staff", () => Results.Ok()).RequireAuthorization(AuthorizationPolicies.StaffOnly);
app.MapGet("/test/manager", () => Results.Ok()).RequireAuthorization(AuthorizationPolicies.ManagerOrAdmin);
app.MapGet("/test/fallback", () => Results.Ok());
app.MapGet("/test/booking-actor", (ClaimsPrincipal user) => Results.Ok(new { isStaff = user.HasStaffRole(), customerId = user.GetCustomerId() }))
    .RequireAuthorization();
app.MapOperationModuleEndpoint();

await using var sql = new NpgsqlConnection(connection);
await sql.OpenAsync();

async Task<object?> ExecuteAsync(string query, params object[] values)
{
    await using var command = new NpgsqlCommand(System.Text.RegularExpressions.Regex.Replace(query, @"\$(\d+)", "@p$1"), sql);
    for (var index = 0; index < values.Length; index++)
        command.Parameters.AddWithValue($"p{index + 1}", values[index]);
    return await command.ExecuteScalarAsync();
}

await using (var scope = app.Services.CreateAsyncScope())
{
    DbContext Context(Type marker, string name) => (DbContext)scope.ServiceProvider.GetRequiredService(
        marker.Assembly.GetTypes().Single(type => type.Name == name));

    var customerDb = Context(typeof(CustomerModule), "CustomerDbContext");
    var operationDb = Context(typeof(OperationModule), "OperationDbContext");
    var authDb = Context(typeof(AuthModule), "AuthDbContext");
    await customerDb.GetService<IMigrator>().MigrateAsync("20261119035358_AlignCustomerProfile");
    await operationDb.GetService<IMigrator>().MigrateAsync("20261018114058_AddAdminStaffRole");
    await authDb.GetService<IMigrator>().MigrateAsync("20261118020243_InitialAuth");
    await ExecuteAsync("""
        INSERT INTO customer.customer (fullname, phone, password_hash, status)
        VALUES ('Customer', $1, $2, 'ACTIVE'), ('Register Me', $3, '', 'ACTIVE'),
               ('Inactive', '0900000005', '', 'INACTIVE');
        INSERT INTO operation.staff (fullname, gender, email, phone, position, role, password_hash, status)
        VALUES ('Legacy Staff', NULL, 'staff@example.test', $4, 'Staff', 'STAFF', $2, 'ACTIVE'),
               ('Admin', 'OTHER', 'admin@example.test', $5, 'Admin', 'ADMIN', $2, 'ACTIVE');
        INSERT INTO petspa_auth."AspNetUsers"
            ("Id", "AccountType", "StaffId", "IsActive", "ProvisioningState", "CreatedAtUtc", "TokenVersion",
             "UserName", "NormalizedUserName", "PasswordHash", "PhoneNumber",
             "EmailConfirmed", "PhoneNumberConfirmed", "TwoFactorEnabled", "LockoutEnabled", "AccessFailedCount")
        VALUES ('legacy-admin', 'staff', 2, true, 'Ready', now(), 5, 'admin', 'ADMIN', $2, $5,
                false, true, false, false, 0);
        """, customerPhone, legacyHash, "+84" + unregisteredPhone[1..], staffPhone, adminPhone);
    await authDb.Database.MigrateAsync();
    await customerDb.Database.MigrateAsync();
    await operationDb.Database.MigrateAsync();
    if ((string?)await ExecuteAsync("SELECT gender FROM operation.staff WHERE id = 1") != "OTHER")
        throw new InvalidOperationException("Legacy staff with unknown gender must survive the Operation migration.");
    if (authDb.GetService<IMigrator>().HasPendingModelChanges() || customerDb.GetService<IMigrator>().HasPendingModelChanges())
        throw new InvalidOperationException("Authentication and Customer models must match their migration snapshots.");
}

await app.StartAsync();
var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
using var client = new HttpClient { BaseAddress = new Uri(address) };
var checks = 0;
// Kiểm tra điều kiện và báo lỗi nếu không đạt.
void Check(bool condition, string description)
{
    if (!condition) throw new InvalidOperationException("FAIL: " + description);
    checks++;
    Console.WriteLine("PASS: " + description);
}
// Gửi yêu cầu HTTP kiểm thử.
async Task<HttpResponseMessage> Send(HttpMethod method, string path, string? token = null, object? body = null)
{
    using var request = new HttpRequestMessage(method, path);
    if (token != null) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
    if (body != null) request.Content = JsonContent.Create(body);
    return await client.SendAsync(request);
}
// Đăng nhập và kiểm tra thông tin token trả về.
async Task<string> Login(string kind, string phone, string role, long id)
{
    using var response = await Send(HttpMethod.Post, "/api/auth/login", body: new { phone, password });
    Check(response.StatusCode == HttpStatusCode.OK, role + " login succeeds");
    var json = await response.Content.ReadFromJsonAsync<JsonElement>();
    Check(json.EnumerateObject().Select(p => p.Name).Order().SequenceEqual(
        new[] { "accessToken", "expiresAtUtc", "role", "tokenType", "user" }.Order()), role + " common response shape");
    Check(json.GetProperty("role").GetString() == role && json.GetProperty("user").GetProperty("id").GetInt64() == id,
        role + " response role and identity");
    Check(json.GetProperty("user").EnumerateObject().Select(p => p.Name).Order().SequenceEqual(new[] { "fullName", "id", "phone" }),
        role + " common user shape excludes password/hash");
    Check(json.GetProperty("tokenType").GetString() == "Bearer" && json.GetProperty("expiresAtUtc").GetDateTime() > DateTime.UtcNow,
        role + " token metadata");
    var token = json.GetProperty("accessToken").GetString()!;
    var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
    Check(jwt.Subject == $"{kind}:{id}" && jwt.Claims.Single(c => c.Type == "role").Value == role
        && !jwt.Claims.Any(c => c.Type == "account_role"), role + " signed role claim matches response");
    Check(jwt.Claims.Single(c => c.Type == "account_type").Value == kind
        && jwt.Claims.Single(c => c.Type == $"{kind}_id").Value == id.ToString(System.Globalization.CultureInfo.InvariantCulture)
        && jwt.Claims.Count(c => c.Type is "staff_id" or "customer_id") == 1
        && jwt.ValidTo == json.GetProperty("expiresAtUtc").GetDateTime(), role + " account claims and exact UTC expiry");
    return token;
}

// Tạo JWT dùng trong kiểm thử.
string IssueTestToken(IEnumerable<Claim> claims, string issuer = "role-tests",
    string audience = "role-tests-client", bool expired = false)
{
    var now = DateTime.UtcNow;
    var jwt = new JwtSecurityToken(issuer, audience, claims,
        now.AddMinutes(-10), expired ? now.AddMinutes(-5) : now.AddMinutes(5),
        new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), SecurityAlgorithms.HmacSha256));
    return new JwtSecurityTokenHandler().WriteToken(jwt);
}
try
{
    using (var anonymous = await Send(HttpMethod.Get, "/test/staff"))
        Check(anonymous.StatusCode == HttpStatusCode.Unauthorized, "anonymous -> 401");
    using (var fallback = await Send(HttpMethod.Get, "/test/fallback"))
        Check(fallback.StatusCode == HttpStatusCode.Unauthorized, "fallback protects endpoint without policy metadata");
    using (var anonymous = await Send(HttpMethod.Get, "/api/customers"))
        Check(anonymous.StatusCode == HttpStatusCode.Unauthorized, "anonymous customer list -> 401");
    using (var anonymous = await Send(HttpMethod.Get, "/api/bookings/1"))
        Check(anonymous.StatusCode == HttpStatusCode.Unauthorized, "anonymous booking -> 401");
    using (var health = await Send(HttpMethod.Get, "/health"))
        Check(health.StatusCode == HttpStatusCode.OK, "health is public");
    using (var catalog = await Send(HttpMethod.Get, "/api/categories"))
        Check(catalog.StatusCode == HttpStatusCode.OK, "category GET is public");
    using (var swaggerUi = await Send(HttpMethod.Get, "/swagger/index.html"))
        Check(swaggerUi.StatusCode == HttpStatusCode.OK, "Swagger UI is public in the smoke host");
    using (var openApi = await Send(HttpMethod.Get, "/swagger/v1/swagger.json"))
        Check(openApi.StatusCode == HttpStatusCode.OK, "Swagger JSON URL serves the generated contract");
    using (var openApi = await Send(HttpMethod.Get, "/openapi/v1.json"))
    {
        Check(openApi.StatusCode == HttpStatusCode.OK, "generated OpenAPI is public");
        var document = await openApi.Content.ReadFromJsonAsync<JsonElement>();
        var paths = document.GetProperty("paths");
        var login = paths.GetProperty("/api/auth/login").GetProperty("post");
        Check(!login.TryGetProperty("security", out var security) || security.GetArrayLength() == 0,
            "unified login is documented as public");
        Check(paths.TryGetProperty("/api/auth/register", out _) && paths.TryGetProperty("/api/auth/logout", out _) &&
            !paths.TryGetProperty("/api/auth/customer/login", out _) && !paths.TryGetProperty("/api/auth/staff/login", out _),
            "generated OpenAPI exposes register, login and logout and removes old login paths");
        var roles = document.GetProperty("components").GetProperty("schemas").GetProperty("LoginResponse")
            .GetProperty("properties").GetProperty("role").GetProperty("enum").EnumerateArray().Select(x => x.GetString());
        Check(roles.SequenceEqual(new[] { "customer", "staff", "receptionist", "manager", "admin" }),
            "generated OpenAPI lists all canonical roles");
        var categories = paths.GetProperty("/api/categories");
        Check(paths.GetProperty("/api/bookings").TryGetProperty("get", out _)
            && paths.GetProperty("/api/bookings/{id}").GetProperty("delete").GetProperty("responses").TryGetProperty("204", out _)
            && paths.GetProperty("/api/booking/{id}").TryGetProperty("patch", out _), "generated OpenAPI exposes the Booking routes");
        var schemas = document.GetProperty("components").GetProperty("schemas");
        var patch = schemas.GetProperty("ChangeBookingStatusRequest");
        Check(!patch.GetProperty("additionalProperties").GetBoolean()
            && patch.GetProperty("properties").GetProperty("status").GetProperty("enum").EnumerateArray()
                .Select(value => value.GetString()).SequenceEqual(new[] { "confirmed", "completed" }),
            "generated OpenAPI documents the strict Booking status body");
        var categoryRead = categories.GetProperty("get");
        Check(!categoryRead.TryGetProperty("security", out var readSecurity) || readSecurity.GetArrayLength() == 0,
            "generated OpenAPI treats category reads as public");
        var writeSecurity = categories.GetProperty("post").GetProperty("security")[0];
        Check(writeSecurity.TryGetProperty("bearerAuth", out _)
            && document.GetProperty("components").GetProperty("securitySchemes").GetProperty("bearerAuth")
                .GetProperty("scheme").GetString() == "bearer", "generated OpenAPI declares bearer auth for protected routes");
    }
    var categoryBody = new { name = "Grooming", status = "ACTIVE" };
    using (var write = await Send(HttpMethod.Post, "/api/categories", body: categoryBody))
        Check(write.StatusCode == HttpStatusCode.Unauthorized && categoryRepository.AddCalls == 0,
            "anonymous category write -> 401");
    var customerToken = await Login("customer", " +84" + customerPhone[1..] + " ", AccountRoles.Customer, 1);
    Check((string?)await ExecuteAsync("SELECT password_hash FROM petspa_auth.accounts WHERE customer_id = 1") != legacyHash, "customer login upgrades migrated hash");
    using (var write = await Send(HttpMethod.Post, "/api/categories", customerToken, categoryBody))
        Check(write.StatusCode == HttpStatusCode.Forbidden && categoryRepository.AddCalls == 0,
            "customer category write -> 403");
    using (var list = await Send(HttpMethod.Get, "/api/customers", customerToken))
        Check(list.StatusCode == HttpStatusCode.Forbidden, "customer cannot list all customers");
    using (var own = await Send(HttpMethod.Get, "/api/customers/1", customerToken))
        Check(own.StatusCode == HttpStatusCode.OK, "customer reads own profile");
    using (var other = await Send(HttpMethod.Get, "/api/customers/999", customerToken))
        Check(other.StatusCode == HttpStatusCode.Forbidden, "customer cannot read another profile");
    using (var own = await Send(HttpMethod.Put, "/api/customers/1", customerToken, new { fullName = "Updated" }))
        Check(own.StatusCode == HttpStatusCode.Created && customerService.LastUpdatedId == 1, "customer updates own profile");
    using (var own = await Send(HttpMethod.Put, "/api/customers/1", customerToken, new { fullName = "Denied", status = (string?)null }))
        Check(own.StatusCode == HttpStatusCode.Forbidden && customerService.UpdateCalls == 1, "customer cannot send status even as null");
    using (var other = await Send(HttpMethod.Put, "/api/customers/999", customerToken, new { fullName = "Updated" }))
        Check(other.StatusCode == HttpStatusCode.Forbidden && customerService.LastUpdatedId == 1,
            "customer cannot update another profile");
    var profileBody = new { fullName = "Created", phone = "0900000999" };
    using (var create = await Send(HttpMethod.Post, "/api/customers", customerToken, profileBody))
        Check(create.StatusCode == HttpStatusCode.Forbidden && customerService.CreateCalls == 0,
            "customer cannot create a customer");
    using (var denied = await Send(HttpMethod.Get, "/test/staff", customerToken))
        Check(denied.StatusCode == HttpStatusCode.Forbidden, "customer rejected by resource staff policy");
    using (var actor = await Send(HttpMethod.Get, "/test/booking-actor", customerToken))
    {
        var json = await actor.Content.ReadFromJsonAsync<JsonElement>();
        Check(!json.GetProperty("isStaff").GetBoolean() && json.GetProperty("customerId").GetInt64() == 1,
            "booking actor uses customer role and customer identity");
    }
    string? receptionistToken = null;
    foreach (var role in Enum.GetValues<StaffRole>())
    {
        await ExecuteAsync("UPDATE operation.staff SET role = $1 WHERE id = 1", role.ToString());
        var token = await Login("staff", staffPhone, role.ToString().ToLowerInvariant(), 1);
        var frontDesk = role is StaffRole.RECEPTIONIST or StaffRole.MANAGER;
        if (role == StaffRole.RECEPTIONIST) receptionistToken = token;
        if (role == StaffRole.STAFF)
            Check((string?)await ExecuteAsync("SELECT password_hash FROM petspa_auth.accounts WHERE staff_id = 1") != legacyHash, "staff login upgrades migrated hash");
        using (var allowed = await Send(HttpMethod.Get, "/test/staff", token))
            Check(allowed.StatusCode == HttpStatusCode.OK, role + " accepted by the shared staff policy");
        using (var manager = await Send(HttpMethod.Get, "/test/manager", token))
            Check(manager.StatusCode == (role is StaffRole.MANAGER or StaffRole.ADMIN ? HttpStatusCode.OK : HttpStatusCode.Forbidden),
                role + " manager/admin RequireRole policy");
        using (var list = await Send(HttpMethod.Get, "/api/customers", token))
            Check(list.StatusCode == (frontDesk ? HttpStatusCode.OK : HttpStatusCode.Forbidden),
                role + " customer list policy");
        using (var profile = await Send(HttpMethod.Get, "/api/customers/999", token))
            Check(profile.StatusCode == (frontDesk ? HttpStatusCode.OK : HttpStatusCode.Forbidden),
                role + " customer management permissions");
        using (var sameId = await Send(HttpMethod.Get, "/api/customers/1", token))
            Check(sameId.StatusCode == (frontDesk ? HttpStatusCode.OK : HttpStatusCode.Forbidden),
                role + " staff ID does not grant customer ownership");
        using (var profile = await Send(HttpMethod.Post, "/api/customers", token, profileBody))
            Check(profile.StatusCode == (frontDesk ? HttpStatusCode.Created : HttpStatusCode.Forbidden),
                role + " create customer policy");
        using (var write = await Send(HttpMethod.Post, "/api/categories", token, categoryBody))
            Check(write.StatusCode == (role == StaffRole.MANAGER ? HttpStatusCode.Created : HttpStatusCode.Forbidden),
                role + " category write policy");
        using (var booking = await Send(HttpMethod.Get, "/api/bookings/2", token))
            Check(booking.StatusCode == (frontDesk ? HttpStatusCode.OK : HttpStatusCode.Forbidden), role + " booking detail policy");
        using (var page = await Send(HttpMethod.Get, "/api/bookings?status=pending&pageSize=1", token))
            Check(page.StatusCode == (frontDesk ? HttpStatusCode.OK : HttpStatusCode.Forbidden), role + " booking list policy");
        using (var list = await Send(HttpMethod.Get, "/api/customers/999/pets", token))
            Check(list.StatusCode == (frontDesk ? HttpStatusCode.OK : HttpStatusCode.Forbidden)
                && (!frontDesk || pets.ListOwner == 999), role + " pet list permissions and requested owner");
        if (role == StaffRole.MANAGER)
        {
            using var update = await Send(HttpMethod.Put, "/api/pets/77", token, new { name = "Denied", specialNote = (string?)null });
            Check(update.StatusCode == HttpStatusCode.Forbidden && pets.UpdateCalls == 0, "manager cannot clear notes or change a second field in the same request");
            using var create = await Send(HttpMethod.Post, "/api/customers/1/pets", token,
                new { customersId = 1, name = "Denied", species = "DOG", specialNote = (string?)null });
            Check(create.StatusCode == HttpStatusCode.Forbidden && pets.CreateCalls == 0, "manager cannot include a special note when creating a pet");
        }
        using (var actor = await Send(HttpMethod.Get, "/test/booking-actor", token))
        {
            var json = await actor.Content.ReadFromJsonAsync<JsonElement>();
            Check(json.GetProperty("isStaff").GetBoolean() && json.GetProperty("customerId").ValueKind == JsonValueKind.Null,
                role + " booking actor is internal staff");
        }
    }
    using (var own = await Send(HttpMethod.Get, "/api/bookings/1", customerToken))
        Check(own.StatusCode == HttpStatusCode.OK, "customer reads own booking");
    using (var other = await Send(HttpMethod.Get, "/api/bookings/2", customerToken))
        Check(other.StatusCode == HttpStatusCode.Forbidden, "customer cannot read another booking");
    using (var create = await Send(HttpMethod.Post, "/api/bookings", customerToken,
        new { customerId = 999, petId = 10, bookingTime = DateTime.UtcNow.AddDays(1), serviceOptionIds = new[] { 1L } }))
        Check(create.StatusCode == HttpStatusCode.Forbidden && bookings.SaveCalls == 0,
            "customer cannot create a booking for another customer");
    using (var replace = await Send(HttpMethod.Put, "/api/bookings/2/services", customerToken, new { serviceOptionIds = new[] { 1L } }))
        Check(replace.StatusCode == HttpStatusCode.Forbidden && bookings.SaveCalls == 0,
            "customer cannot change services for another booking");
    foreach (var (owner, status) in new[] { (999L, "PENDING"), (1L, "CONFIRMED") })
    {
        using var update = await Send(HttpMethod.Put, "/api/bookings/1", customerToken,
            new { customerId = owner, petId = 10, bookingTime = DateTime.UtcNow.AddDays(1), status });
        Check(update.StatusCode == HttpStatusCode.Forbidden && bookings.SaveCalls == 0,
            owner == 999 ? "customer cannot change booking owner" : "customer cannot change booking status");
    }
    using (var create = await Send(HttpMethod.Post, "/api/bookings", customerToken,
        new { customerId = 1L, petId = 10L, bookingTime = DateTime.UtcNow.AddDays(1), serviceOptionIds = new[] { 1L } }))
        Check(create.StatusCode == HttpStatusCode.Forbidden && bookings.SaveCalls == 0, "customer cannot book for their own pet");
    await ExecuteAsync("UPDATE operation.staff SET role = 'RECEPTIONIST' WHERE id = 1");
    using (var create = await Send(HttpMethod.Post, "/api/bookings", receptionistToken,
        new { customerId = 1L, petId = 20L, bookingTime = DateTime.UtcNow.AddDays(1), serviceOptionIds = new[] { 1L } }))
        Check(create.StatusCode == HttpStatusCode.BadRequest && bookings.SaveCalls == 0, "booking rejects a pet of another customer");
    using (var update = await Send(HttpMethod.Put, "/api/bookings/1", receptionistToken, new { contactName = "Valid", customerId = 2 }))
        Check(update.StatusCode == HttpStatusCode.BadRequest && bookings.SaveCalls == 0, "booking PUT rejects unknown members before writing");
    using (var patch = await Send(HttpMethod.Patch, "/api/booking/1", receptionistToken, new { status = "confirmed", note = "extra" }))
        Check(patch.StatusCode == HttpStatusCode.BadRequest && bookings.SaveCalls == 0, "booking PATCH rejects unknown members before writing");
    using (var invalidFilter = await Send(HttpMethod.Get, "/api/bookings?status=42", receptionistToken))
        Check(invalidFilter.StatusCode == HttpStatusCode.BadRequest, "booking rejects unknown numeric status filters");
    using (var create = await Send(HttpMethod.Post, "/api/bookings", receptionistToken,
        new { customerId = 1L, petId = 10L, bookingTime = DateTime.UtcNow.AddDays(1), serviceOptionIds = new[] { 1L } }))
        Check(create.StatusCode == HttpStatusCode.Created && bookings.SaveCalls == 1, "receptionist can create a booking");
    using (var list = await Send(HttpMethod.Get, "/api/customers/1/pets", customerToken))
        Check(list.StatusCode == HttpStatusCode.OK && pets.ListOwner == 1, "customer lists own pets");
    using (var list = await Send(HttpMethod.Get, "/api/customers/999/pets", customerToken))
        Check(list.StatusCode == HttpStatusCode.Forbidden && pets.ListOwner == 1, "customer cannot list another customer's pets");
    using (var create = await Send(HttpMethod.Post, "/api/customers/999/pets", customerToken,
        new { customersId = 999, name = "Not allowed", species = "DOG" }))
        Check(create.StatusCode == HttpStatusCode.Forbidden && pets.CreateCalls == 0,
            "customer cannot create a pet for another customer");
    using (var read = await Send(HttpMethod.Get, "/api/pets/77", customerToken))
        Check(read.StatusCode == HttpStatusCode.NotFound, "pet read validates actual owner");
    using (var update = await Send(HttpMethod.Put, "/api/pets/77", customerToken, new { name = "Not allowed" }))
        Check(update.StatusCode == HttpStatusCode.Forbidden && pets.UpdateCalls == 0, "customer cannot update a pet");
    using (var create = await Send(HttpMethod.Post, "/api/customers/1/pets", receptionistToken,
        new { customersId = 999, name = "Not allowed", species = Species.DOG }))
        Check(create.StatusCode == HttpStatusCode.BadRequest && pets.CreateCalls == 0, "pet creation rejects mismatched body owner");
    foreach (var kind in new[] { "customer", "staff" })
    {
        var phone = kind == "customer" ? customerPhone : staffPhone;
        using var wrong = await Send(HttpMethod.Post, "/api/auth/login", body: new { phone, password = "wrong" });
        Check(wrong.StatusCode == HttpStatusCode.Unauthorized, kind + " wrong password -> 401");
        using var invalid = await Send(HttpMethod.Post, "/api/auth/login", body: new { phone = "", password });
        Check(invalid.StatusCode == HttpStatusCode.BadRequest, kind + " invalid input -> 400");
        using var missing = await Send(HttpMethod.Post, "/api/auth/login", body: new { phone = "0900000099", password });
        Check(missing.StatusCode == HttpStatusCode.Unauthorized, kind + " unknown account -> 401");
    }
    await ExecuteAsync("UPDATE customer.customer SET status = 'INACTIVE' WHERE \"Id\" = 1; UPDATE operation.staff SET status = 'INACTIVE' WHERE id = 1;");
    foreach (var inactivePhone in new[] { customerPhone, staffPhone })
    {
        using var inactive = await Send(HttpMethod.Post, "/api/auth/login", body: new { phone = inactivePhone, password });
        Check(inactive.StatusCode == HttpStatusCode.Unauthorized, "inactive profile cannot log in");
    }
    using (var inactiveToken = await Send(HttpMethod.Get, "/test/fallback", customerToken))
        Check(inactiveToken.StatusCode == HttpStatusCode.Unauthorized, "existing token is rejected after profile deactivation");
    await ExecuteAsync("UPDATE customer.customer SET status = 'ACTIVE' WHERE \"Id\" = 1;");
    await ExecuteAsync("UPDATE operation.staff SET status = 'ACTIVE', role = 'MANAGER' WHERE id = 1;");
    var managerToken = await Login("staff", staffPhone, AccountRoles.Manager, 1);
    var adminToken = await Login("staff", adminPhone, AccountRoles.Admin, 2);
    using (var activationDenied = await Send(HttpMethod.Post, "/api/auth/customers/2/activation", customerToken))
        Check(activationDenied.StatusCode == HttpStatusCode.Forbidden, "customer cannot issue activation");
    using (var inactiveActivation = await Send(HttpMethod.Post, "/api/auth/customers/3/activation", managerToken))
        Check(inactiveActivation.StatusCode == HttpStatusCode.NotFound, "inactive customer cannot get activation");
    using var issue = await Send(HttpMethod.Post, "/api/auth/customers/2/activation", managerToken);
    Check(issue.StatusCode == HttpStatusCode.OK, "front desk issues activation for an existing customer");
    var activation = (await issue.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("activationToken").GetString();
    using (var wrongPhone = await Send(HttpMethod.Post, "/api/auth/register",
        body: new { phone = customerPhone, password, activationToken = activation }))
        Check(wrongPhone.StatusCode == HttpStatusCode.BadRequest, "activation is bound to its customer and phone");
    using (var wrongCode = await Send(HttpMethod.Post, "/api/auth/register",
        body: new { phone = unregisteredPhone, password, activationToken = "invalid" }))
        Check(wrongCode.StatusCode == HttpStatusCode.BadRequest, "invalid activation rejected");
    var registrationBody = new { phone = "+84" + unregisteredPhone[1..], password, activationToken = activation };
    var registrations = await Task.WhenAll(
        Send(HttpMethod.Post, "/api/auth/register", body: registrationBody),
        Send(HttpMethod.Post, "/api/auth/register", body: registrationBody));
    using (registrations[0])
    using (registrations[1])
    {
        Check(registrations.Count(x => x.StatusCode == HttpStatusCode.Created) == 1 &&
            registrations.Count(x => x.StatusCode == HttpStatusCode.Conflict) == 1, "only one concurrent registration succeeds");
        var registered = await registrations.Single(x => x.StatusCode == HttpStatusCode.Created).Content.ReadFromJsonAsync<JsonElement>();
        Check(registered.GetProperty("customerId").GetInt64() == 2 &&
            (long)(await ExecuteAsync("SELECT count(*) FROM customer.customer"))! == 3,
            "registration links the existing customer profile");
    }

    var deviceOne = await Login("customer", unregisteredPhone, AccountRoles.Customer, 2);
    var deviceTwo = await Login("customer", unregisteredPhone, AccountRoles.Customer, 2);
    using (var logout = await Send(HttpMethod.Post, "/api/auth/logout", deviceOne))
        Check(logout.StatusCode == HttpStatusCode.NoContent, "server-side logout succeeds");
    foreach (var revoked in new[] { deviceOne, deviceTwo })
    {
        using var use = await Send(HttpMethod.Get, "/test/fallback", revoked);
        Check(use.StatusCode == HttpStatusCode.Unauthorized, "logout revokes previous tokens on every device");
    }
    var newSession = await Login("customer", unregisteredPhone, AccountRoles.Customer, 2);
    using (var use = await Send(HttpMethod.Get, "/test/fallback", newSession))
        Check(use.StatusCode == HttpStatusCode.OK, "login again after logout creates a valid session");

    using (var denied = await Send(HttpMethod.Patch, "/api/auth/staff-accounts/2", managerToken, new { password }))
        Check(denied.StatusCode == HttpStatusCode.Forbidden, "manager cannot change an admin account");
    using (var denied = await Send(HttpMethod.Put, "/api/staff/2", managerToken, new { role = "STAFF" }))
        Check(denied.StatusCode == HttpStatusCode.Forbidden, "manager cannot demote an admin through Operation");
    using (var lastAdminAccount = await Send(HttpMethod.Patch, "/api/auth/staff-accounts/2", adminToken, new { isEnabled = false }))
        Check(lastAdminAccount.StatusCode == HttpStatusCode.Conflict, "last admin account cannot be disabled");
    using (var lastAdminProfile = await Send(HttpMethod.Delete, "/api/staff/2", adminToken))
        Check(lastAdminProfile.StatusCode == HttpStatusCode.Conflict, "last admin profile cannot be deactivated");
    using (var missing = await Send(HttpMethod.Post, "/api/auth/staff-accounts", managerToken,
        new { staffId = long.MaxValue, password }))
        Check(missing.StatusCode == HttpStatusCode.NotFound, "staff account requires an existing profile");

    using var createRoom = await Send(HttpMethod.Post, "/api/room", managerToken,
        new { roomName = "QA room", roomType = "BATH" });
    Check(createRoom.StatusCode == HttpStatusCode.OK, "manager creates a room");
    var rooms = await createRoom.Content.ReadFromJsonAsync<JsonElement>();
    var roomId = rooms.EnumerateArray().Single(x => x.GetProperty("roomName").GetString() == "QA room")
        .GetProperty("id").GetInt64();
    using (var change = await Send(HttpMethod.Put, $"/api/room/{roomId}", managerToken, new { status = "MAINTENANCE" }))
        Check(change.StatusCode == HttpStatusCode.OK, "manager changes room status");
    using (var list = await Send(HttpMethod.Get, "/api/room", managerToken))
        Check(list.StatusCode == HttpStatusCode.OK, "manager lists rooms");

    const string newStaffPhone = "0900000006";
    using var createProfile = await Send(HttpMethod.Post, "/api/staff", managerToken, new
    {
        fullName = "New Receptionist", gender = "OTHER", email = "new-staff@example.test", phone = newStaffPhone,
        position = "Front desk", role = "RECEPTIONIST", status = "ACTIVE"
    });
    Check(createProfile.StatusCode == HttpStatusCode.Created, "manager creates the Operation staff profile");
    var newStaffId = (await createProfile.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt64();
    using (var create = await Send(HttpMethod.Post, "/api/auth/staff-accounts", managerToken, new { staffId = newStaffId, password }))
        Check(create.StatusCode == HttpStatusCode.Created, "manager provisions an account for the staff profile");
    using (var duplicate = await Send(HttpMethod.Post, "/api/auth/staff-accounts", managerToken, new { staffId = newStaffId, password }))
        Check(duplicate.StatusCode == HttpStatusCode.Conflict, "staff account cannot be provisioned twice");
    var newStaffToken = await Login("staff", newStaffPhone, AccountRoles.Receptionist, newStaffId);
    using (var modify = await Send(HttpMethod.Put, $"/api/staff/{newStaffId}", managerToken, new { role = "STAFF" }))
        Check(modify.StatusCode == HttpStatusCode.OK, "manager changes a permitted role in Operation");
    using (var oldRole = await Send(HttpMethod.Get, "/test/staff", newStaffToken))
        Check(oldRole.StatusCode == HttpStatusCode.Unauthorized, "old role token rejected after Operation update");
    newStaffToken = await Login("staff", newStaffPhone, AccountRoles.Staff, newStaffId);
    var shiftDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));
    var shiftBody = new { staffId = newStaffId, shiftDate, startTime = "09:00:00", endTime = "12:00:00" };
    using (var denied = await Send(HttpMethod.Post, "/api/staff-shifts", newStaffToken, shiftBody))
        Check(denied.StatusCode == HttpStatusCode.Forbidden, "staff cannot create a shift");
    using var createShift = await Send(HttpMethod.Post, "/api/staff-shifts", managerToken, shiftBody);
    Check(createShift.StatusCode == HttpStatusCode.Created, "manager creates a scheduled shift");
    var shiftId = (await createShift.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt64();
    var shiftPath = $"/api/staff-shifts/{shiftId}";
    using (var change = await Send(HttpMethod.Put, shiftPath, managerToken,
        new { shiftDate, startTime = "09:30:00", endTime = "12:00:00" }))
        Check(change.StatusCode == HttpStatusCode.OK, "manager adjusts a scheduled shift");
    using (var list = await Send(HttpMethod.Get, $"/api/staff-shifts?staffId={newStaffId}", managerToken))
        Check(list.StatusCode == HttpStatusCode.OK, "manager lists shifts");
    using (var start = await Send(HttpMethod.Post, shiftPath + "/start", newStaffToken))
        Check(start.StatusCode == HttpStatusCode.OK, "staff starts their shift");
    using (var finish = await Send(HttpMethod.Post, shiftPath + "/finish", newStaffToken))
        Check(finish.StatusCode == HttpStatusCode.OK, "staff completes their shift");
    using (var cancel = await Send(HttpMethod.Delete, shiftPath, managerToken))
        Check(cancel.StatusCode == HttpStatusCode.Conflict, "a completed shift cannot be cancelled");
    using var secondShift = await Send(HttpMethod.Post, "/api/staff-shifts", managerToken,
        new { staffId = newStaffId, shiftDate, startTime = "13:00:00", endTime = "16:00:00" });
    Check(secondShift.StatusCode == HttpStatusCode.Created, "manager creates a second shift");
    var secondShiftId = (await secondShift.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt64();
    using (var cancel = await Send(HttpMethod.Delete, $"/api/staff-shifts/{secondShiftId}", managerToken))
        Check(cancel.StatusCode == HttpStatusCode.NoContent, "manager cancels an eligible scheduled shift");

    const string changedPassword = "New-test-passphrase-234";
    using (var patch = await Send(HttpMethod.Patch, $"/api/auth/staff-accounts/{newStaffId}", managerToken,
        new { password = changedPassword, isEnabled = false }))
        Check(patch.StatusCode == HttpStatusCode.NoContent, "manager resets password and disables account");
    using (var old = await Send(HttpMethod.Get, "/test/staff", newStaffToken))
        Check(old.StatusCode == HttpStatusCode.Unauthorized, "staff account change revokes old token");
    using (var disabled = await Send(HttpMethod.Post, "/api/auth/login", body: new { phone = newStaffPhone, password = changedPassword }))
        Check(disabled.StatusCode == HttpStatusCode.Unauthorized, "disabled account cannot log in");
    using (var patch = await Send(HttpMethod.Patch, $"/api/auth/staff-accounts/{newStaffId}", managerToken, new { isEnabled = true }))
        Check(patch.StatusCode == HttpStatusCode.NoContent, "manager enables the staff account");
    using (var updated = await Send(HttpMethod.Post, "/api/auth/login", body: new { phone = newStaffPhone, password = changedPassword }))
        Check(updated.StatusCode == HttpStatusCode.OK, "updated password works after the account is enabled");


    var legacyClaims = new Claim[]
    {
        new("sub", "staff:1"), new("account_type", "staff"), new("staff_id", "1"), new("account_role", "MANAGER")
    };
    using (var legacy = await Send(HttpMethod.Get, "/test/staff", IssueTestToken(legacyClaims)))
        Check(legacy.StatusCode == HttpStatusCode.Unauthorized, "legacy claims cannot bypass the role policy");
    // Tạo bộ claim nhân viên để kiểm thử.
    Claim[] StaffClaims(string role = "staff", string accountType = "staff", string id = "1", string sub = "staff:1") =>
        [new("role", role), new("account_type", accountType), new("staff_id", id), new("sub", sub)];
    var malformedAccounts = new (string Description, Claim[] Claims)[]
    {
        ("account type alone", [new("account_type", "staff")]),
        ("role alone", [new("role", "staff")]),
        ("uppercase role", StaffClaims(role: "STAFF")),
        ("unknown role", StaffClaims(role: "unknown")),
        ("role/type mismatch", StaffClaims(role: "customer")),
        ("subject mismatch", StaffClaims(sub: "customer:1")),
        ("non-numeric ID", StaffClaims(id: "invalid")),
        ("negative ID", StaffClaims(id: "-1", sub: "staff:-1")),
        ("zero ID", StaffClaims(id: "0", sub: "staff:0")),
        ("duplicate role", [.. StaffClaims(), new("role", "staff")]),
        ("duplicate staff ID", [.. StaffClaims(), new("staff_id", "1")]),
        ("both ID types", [.. StaffClaims(), new("customer_id", "1")])
    };
    foreach (var (description, claims) in malformedAccounts)
    {
        var token = IssueTestToken(claims);
        using var booking = await Send(HttpMethod.Get, "/api/bookings/1", token);
        using var fallback = await Send(HttpMethod.Get, "/test/fallback", token);
        using var write = await Send(HttpMethod.Post, "/api/categories", token, categoryBody);
        Check(booking.StatusCode == HttpStatusCode.Unauthorized && fallback.StatusCode == HttpStatusCode.Unauthorized
            && write.StatusCode == HttpStatusCode.Unauthorized, description + " -> 401 on account, fallback and staff policies");
    }
    foreach (var (description, token) in new[]
    {
        ("expired token", IssueTestToken(StaffClaims(), expired: true)),
        ("non-string JWT subject", IssueTestToken([.. StaffClaims(), new("sub", "staff:1")])),
        ("wrong issuer", IssueTestToken(StaffClaims(), issuer: "other-issuer")),
        ("wrong audience", IssueTestToken(StaffClaims(), audience: "other-audience"))
    })
    {
        using var response = await Send(HttpMethod.Get, "/api/bookings/1", token);
        Check(response.StatusCode == HttpStatusCode.Unauthorized, description + " -> 401");
    }
    var parts = customerToken.Split('.');
    parts[2] = (parts[2][0] == 'A' ? "B" : "A") + parts[2][1..];
    using (var tampered = await Send(HttpMethod.Get, "/api/customers/1", string.Join('.', parts)))
        Check(tampered.StatusCode == HttpStatusCode.Unauthorized, "tampered JWT -> 401");
    await using var db = new OperationDbContext(new DbContextOptionsBuilder<OperationDbContext>()
        .UseNpgsql("Host=127.0.0.1;Database=unused;Username=synthetic;Password=synthetic").Options);
    Check(db.GetService<IDesignTimeModel>().Model.FindEntityType(typeof(Staff))!.GetCheckConstraints()
        .Single(c => c.Name == "ck_staff_role").Sql.Contains("'ADMIN'"),
        "staff model permits ADMIN");
    Check(db.Database.GetMigrations().Any(m => m.EndsWith("_AddAdminStaffRole")), "admin role migration is discoverable");
    Check(!db.GetService<IMigrator>().HasPendingModelChanges(), "operation model matches migration snapshot");
    Console.WriteLine($"RESULT: {checks} checks passed.");
}
finally { await app.StopAsync(); }
return 0;

sealed class FakePets : IPetServices
{
    public long? ListOwner { get; private set; }
    public int UpdateCalls { get; private set; }
    public int CreateCalls { get; private set; }
    // Tạo dữ liệu thú cưng mẫu.
    static PetResponse Pet(long id, long owner) => new(id, owner, "Pet", Species.DOG, null, null, null, null, PetStatus.ACTIVE, null);
    // Tìm đối tượng theo mã.
    public Task<Result<PetResponse>> GetByIdAsync(long id, CancellationToken ct) => Task.FromResult(Result<PetResponse>.Success(Pet(id, 999)));
    // Lấy danh sách theo mã liên quan.
    public Task<Result<List<PetResponse>>> GetAllByIdAsync(long id, CancellationToken ct)
    { ListOwner = id; return Task.FromResult(Result<List<PetResponse>>.Success([])); }
    // Tạo đối tượng mới.
    public Task<Result<PetResponse>> CreateAsync(CreatePetRequest req, CancellationToken ct)
    { CreateCalls++; return Task.FromResult(Result<PetResponse>.Success(Pet(78, req.CustomersId))); }
    // Cập nhật đối tượng.
    public Task<Result<PetResponse>> UpdateAsync(long id, UpdatePetRequest req, CancellationToken ct)
    { UpdateCalls++; return Task.FromResult(Result<PetResponse>.Success(Pet(id, 999))); }
}
sealed class FakeCustomerService : ICustomerService
{
    public Task<Result<List<CustomerResponse>>> GetAsync(CancellationToken ct)
        => Task.FromResult(Result<List<CustomerResponse>>.Success([Customer(1)]));
    public long? LastUpdatedId { get; private set; }
    public int UpdateCalls { get; private set; }
    public int CreateCalls { get; private set; }
    // Tạo dữ liệu khách hàng mẫu.
    static CustomerResponse Customer(long id) => new(id, "Customer", null, null, null, "0900000001", null, null, []);
    // Tìm đối tượng theo mã.
    public Task<Result<CustomerResponse>> GetByIdAsync(long id, CancellationToken ct) => Task.FromResult(Result<CustomerResponse>.Success(Customer(id)));
    // Tạo đối tượng mới.
    public Task<Result<CustomerResponse>> CreateAsync(CreateCustomerRequest req, CancellationToken ct)
    { CreateCalls++; return Task.FromResult(Result<CustomerResponse>.Success(Customer(999))); }
    // Cập nhật đối tượng.
    public Task<Result<CustomerResponse>> UpdateAsync(long id, UpdateCustomerRequest req, CancellationToken ct)
    { UpdateCalls++; LastUpdatedId = id; return Task.FromResult(Result<CustomerResponse>.Success(Customer(id))); }
}

sealed class FakeBookings : IBookingRepository
{
    private readonly Dictionary<long, Booking> records = new()
    {
        [1] = new Booking { Id = 1, CustomerId = 1, PetId = 10, BookingTime = DateTime.UtcNow.AddDays(1) },
        [2] = new Booking { Id = 2, CustomerId = 999, PetId = 20, BookingTime = DateTime.UtcNow.AddDays(1) }
    };

    public int SaveCalls { get; private set; }
    // Tìm đối tượng theo mã.
    public Task<Booking?> GetByIdAsync(long id, CancellationToken ct) => Task.FromResult(records.GetValueOrDefault(id));
    public Task<Booking?> GetForUpdateAsync(long id, CancellationToken ct) => GetByIdAsync(id, ct);
    public Task<IReadOnlyList<Booking>> ReadPageAsync(BookingListQuery query, BookingCursor? after, int limit, CancellationToken ct)
        => Task.FromResult<IReadOnlyList<Booking>>([]);
    public Task<T> InTransactionAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken ct) => action(ct);
    // Thêm đối tượng mới.
    public void Add(Booking booking) => records.Add(booking.Id = records.Count + 1, booking);
    // Xóa chi tiết lịch hẹn.
    public void RemoveDetail(BookingDetail detail) { }
    // Ghi nhận số lần lưu trong dữ liệu giả.
    public Task SaveChangesAsync(CancellationToken ct) { SaveCalls++; return Task.CompletedTask; }
}

sealed class FakeCustomerModule : ICustomerModule
{
    // Kiểm tra thú cưng thuộc khách hàng.
    public Task<bool> PetBelongsToCustomerAsync(long petId, long customerId, CancellationToken ct) =>
        Task.FromResult((petId, customerId) is (10, 1) or (20, 999));
    public async Task<BookingCustomerDto?> GetBookingCustomerAsync(long customerId, long petId, CancellationToken ct) =>
        await PetBelongsToCustomerAsync(petId, customerId, ct) ? new(customerId, petId, "Customer", "0900000001", "DOG", 5) : null;
    public Task<BookingContactDto?> GetBookingContactAsync(long customerId, CancellationToken ct) =>
        Task.FromResult<BookingContactDto?>(new(customerId, "Customer", "0900000001"));
}

sealed class FakeResourceModule : IResourceModule
{
    // Lấy các tùy chọn dịch vụ đang hoạt động.
    public Task<IReadOnlyList<BookableServiceOption>> GetActiveServiceOptionsAsync(
        IReadOnlyCollection<long> ids, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<BookableServiceOption>>([new(1, 100_000) { DurationMinutes = 30 }]);
    public Task<IReadOnlyList<ServiceOptionDescription>> GetServiceOptionDescriptionsAsync(
        IReadOnlyCollection<long> ids, CancellationToken ct) => Task.FromResult<IReadOnlyList<ServiceOptionDescription>>([]);
}

sealed class FakeCategories : ICategoryRepository
{
    public int AddCalls { get; private set; }
    // Lấy danh sách đối tượng.
    public Task<IReadOnlyList<Category>> ListAsync(CancellationToken ct) => Task.FromResult<IReadOnlyList<Category>>([]);
    // Tìm đối tượng theo mã.
    public Task<Category?> GetByIdAsync(long id, CancellationToken ct) => Task.FromResult<Category?>(null);
    // Kiểm tra tên đã tồn tại.
    public Task<bool> NameExistsAsync(string name, long? exceptId, CancellationToken ct) => Task.FromResult(false);
    // Thêm đối tượng mới.
    public void Add(Category category) => category.Id = ++AddCalls;
    // Giả lập lưu thay đổi thành công.
    public Task<bool> TrySaveChangesAsync(CancellationToken ct) => Task.FromResult(true);
}

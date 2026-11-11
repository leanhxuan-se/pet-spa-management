using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PetSpa.Api.Composition;
using PetSpa.Modules.Customer.Application.Abstractions;
using PetSpa.Modules.Customer.Application.Services;
using PetSpa.Modules.Customer.Domain.Entities;
using PetSpa.Modules.Customer.Domain.Repositories;
using PetSpa.Modules.Customer.Endpoints;
using PetSpa.Modules.Customer.Endpoints.Requests;
using PetSpa.Modules.Customer.Endpoints.Responses;
using PetSpa.Modules.Operation.Application.Services;
using PetSpa.Modules.Operation.Domain.Entities;
using PetSpa.Modules.Operation.Domain.Enums;
using PetSpa.Modules.Operation.Domain.Repositories;
using PetSpa.Modules.Operation.Endpoints;
using PetSpa.Modules.Operation.Endpoints.Requests;
using PetSpa.Modules.Operation.Infrastructure.Persistence;
using PetSpa.Modules.Resource.Endpoints;
using PetSpa.SharedKernel.Application.Abstractions;
using PetSpa.SharedKernel.Application.Authentication;
using PetSpa.SharedKernel.Application.Validators;
using CustomerStatus = PetSpa.Modules.Customer.Domain.Enums.CustomerStatus;
using PetStatus = PetSpa.Modules.Customer.Domain.Enums.PetStatus;
using Species = PetSpa.Modules.Customer.Domain.Enums.Species;

const string key = "smoke-tests-only-synthetic-signing-key-0123456789";
const string password = "Smoke-tests-only-passphrase!123";
var customer = new Customer { Id = 1, FullName = "Customer", Phone = "0900000001", Status = CustomerStatus.ACTIVE };
var staff = new Staff { Id = 2, FullName = "Staff", Phone = "0900000002", Status = StaffStatus.ACTIVE };
customer.PasswordHash = new PasswordHasher<Customer>().HashPassword(customer, password);
staff.PasswordHash = new PasswordHasher<Staff>().HashPassword(staff, password);
var pets = new FakePets();
var builder = WebApplication.CreateBuilder(new WebApplicationOptions { Args = [], EnvironmentName = "Production" });
builder.Configuration.Sources.Clear();
builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
{
    ["Jwt:Issuer"] = "role-tests", ["Jwt:Audience"] = "role-tests-client", ["Jwt:SigningKey"] = key
});
builder.Logging.ClearProviders();
builder.WebHost.UseUrls("http://127.0.0.1:0");
builder.Services.AddAuthenticationServices(builder.Configuration);
builder.Services.AddSingleton<ICustomerRepository>(new FakeCustomers(customer));
builder.Services.AddSingleton<IStaffRepository>(new FakeStaffs(staff));
builder.Services.AddSingleton<IPetServices>(pets);
builder.Services.AddSingleton<ICustomerService, FakeCustomerService>();
builder.Services.AddScoped<CustomerLoginService>();
builder.Services.AddScoped<StaffLoginService>();
builder.Services.AddScoped<IValidator<CustomerLoginRequest>, LoginRequestValidator<CustomerLoginRequest>>();
builder.Services.AddScoped<IValidator<StaffLoginRequest>, LoginRequestValidator<StaffLoginRequest>>();
await using var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();
CustomerAuthEndpoints.MapCustomerAuthEndpoints(app);
app.MapOperationAuthEndpoints();
app.MapCustomerEndpoint();
app.MapPetEndpoint();
app.MapGet("/test/staff", () => Results.Ok()).RequireStaff();
app.MapGet("/test/manager", () => Results.Ok()).RequireAuthorization(p => p.RequireRole(AccountRoles.Manager, AccountRoles.Admin));
app.MapGet("/test/booking-actor", (ClaimsPrincipal user) => Results.Ok(new { isStaff = user.HasStaffRole(), customerId = user.GetCustomerId() }))
    .RequireAuthorization();
await app.StartAsync();
var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
using var client = new HttpClient { BaseAddress = new Uri(address) };
var checks = 0;
void Check(bool condition, string description)
{
    if (!condition) throw new InvalidOperationException("FAIL: " + description);
    checks++;
    Console.WriteLine("PASS: " + description);
}
async Task<HttpResponseMessage> Send(HttpMethod method, string path, string? token = null, object? body = null)
{
    using var request = new HttpRequestMessage(method, path);
    if (token != null) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
    if (body != null) request.Content = JsonContent.Create(body);
    return await client.SendAsync(request);
}
async Task<string> Login(string kind, string phone, string role, long id)
{
    using var response = await Send(HttpMethod.Post, $"/api/{kind}/login", body: new { phone, password });
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
    return token;
}
try
{
    using (var anonymous = await Send(HttpMethod.Get, "/test/staff"))
        Check(anonymous.StatusCode == HttpStatusCode.Unauthorized, "anonymous -> 401");
    var customerToken = await Login("customer", " " + customer.Phone + " ", AccountRoles.Customer, 1);
    using (var own = await Send(HttpMethod.Get, "/api/customer/1", customerToken))
        Check(own.StatusCode == HttpStatusCode.OK, "customer reads own profile");
    using (var other = await Send(HttpMethod.Get, "/api/customer/999", customerToken))
        Check(other.StatusCode == HttpStatusCode.Forbidden, "customer cannot read another profile");
    using (var denied = await Send(HttpMethod.Get, "/test/staff", customerToken))
        Check(denied.StatusCode == HttpStatusCode.Forbidden, "customer rejected by resource staff policy");
    using (var actor = await Send(HttpMethod.Get, "/test/booking-actor", customerToken))
    {
        var json = await actor.Content.ReadFromJsonAsync<JsonElement>();
        Check(!json.GetProperty("isStaff").GetBoolean() && json.GetProperty("customerId").GetInt64() == 1,
            "booking actor uses customer role and customer identity");
    }
    foreach (var role in Enum.GetValues<StaffRole>())
    {
        staff.Role = role;
        var token = await Login("staff", staff.Phone, role.ToString().ToLowerInvariant(), 2);
        using (var allowed = await Send(HttpMethod.Get, "/test/staff", token))
            Check(allowed.StatusCode == HttpStatusCode.OK, role + " accepted by resource staff policy");
        using (var manager = await Send(HttpMethod.Get, "/test/manager", token))
            Check(manager.StatusCode == (role is StaffRole.MANAGER or StaffRole.ADMIN ? HttpStatusCode.OK : HttpStatusCode.Forbidden),
                role + " manager/admin RequireRole policy");
        using (var profile = await Send(HttpMethod.Get, "/api/customer/999", token))
            Check(profile.StatusCode == (role == StaffRole.STAFF ? HttpStatusCode.Forbidden : HttpStatusCode.OK),
                role + " customer management permissions");
        using (var list = await Send(HttpMethod.Get, "/api/pet/all_pets/999", token))
            Check(list.StatusCode == (role == StaffRole.STAFF ? HttpStatusCode.Forbidden : HttpStatusCode.OK)
                && (role == StaffRole.STAFF || pets.ListOwner == 999), role + " pet list permissions and requested owner");
        using (var actor = await Send(HttpMethod.Get, "/test/booking-actor", token))
        {
            var json = await actor.Content.ReadFromJsonAsync<JsonElement>();
            Check(json.GetProperty("isStaff").GetBoolean() && json.GetProperty("customerId").ValueKind == JsonValueKind.Null,
                role + " booking actor is internal staff");
        }
    }
    using (var read = await Send(HttpMethod.Get, "/api/pet/1/77", customerToken))
        Check(read.StatusCode == HttpStatusCode.NotFound, "pet read validates actual owner");
    using (var update = await Send(HttpMethod.Put, "/api/pet/update/1/77", customerToken, new { name = "Not allowed" }))
        Check(update.StatusCode == HttpStatusCode.NotFound && pets.UpdateCalls == 0, "pet update rejects another owner's pet");
    using (var create = await Send(HttpMethod.Post, "/api/pet/create/1", customerToken,
        new { customersId = 999, name = "Not allowed", species = Species.DOG }))
        Check(create.StatusCode == HttpStatusCode.BadRequest && pets.CreateCalls == 0, "pet creation rejects mismatched body owner");
    foreach (var kind in new[] { "customer", "staff" })
    {
        var phone = kind == "customer" ? customer.Phone : staff.Phone;
        using var wrong = await Send(HttpMethod.Post, $"/api/{kind}/login", body: new { phone, password = "wrong" });
        Check(wrong.StatusCode == HttpStatusCode.Unauthorized, kind + " wrong password -> 401");
        using var invalid = await Send(HttpMethod.Post, $"/api/{kind}/login", body: new { phone = "", password });
        Check(invalid.StatusCode == HttpStatusCode.BadRequest, kind + " invalid input -> 400");
    }
    staff.Role = (StaffRole)999;
    using (var invalidRole = await Send(HttpMethod.Post, "/api/staff/login", body: new { phone = staff.Phone, password }))
        Check(invalidRole.StatusCode == HttpStatusCode.Unauthorized, "unknown staff role cannot receive a token");
    var jwt = new JwtSecurityToken("role-tests", "role-tests-client",
        [new Claim("account_type", "staff"), new Claim("account_role", "MANAGER")],
        DateTime.UtcNow.AddSeconds(-1), DateTime.UtcNow.AddMinutes(5),
        new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), SecurityAlgorithms.HmacSha256));
    using (var legacy = await Send(HttpMethod.Get, "/test/staff", new JwtSecurityTokenHandler().WriteToken(jwt)))
        Check(legacy.StatusCode == HttpStatusCode.Forbidden, "legacy claims cannot bypass the role policy");
    var parts = customerToken.Split('.');
    parts[2] = (parts[2][0] == 'A' ? "B" : "A") + parts[2][1..];
    using (var tampered = await Send(HttpMethod.Get, "/api/customer/1", string.Join('.', parts)))
        Check(tampered.StatusCode == HttpStatusCode.Unauthorized, "tampered JWT -> 401");
    await using var db = new OperationDbContext(new DbContextOptionsBuilder<OperationDbContext>()
        .UseNpgsql("Host=127.0.0.1;Database=unused;Username=synthetic;Password=synthetic").Options);
    Check(db.Model.FindEntityType(typeof(Staff))!.GetCheckConstraints().Single(c => c.Name == "ck_staff_role").Sql.Contains("'ADMIN'"),
        "staff model permits ADMIN");
    Check(db.Database.GetMigrations().Any(m => m.EndsWith("_AddAdminStaffRole")), "admin role migration is discoverable");
    Console.WriteLine($"RESULT: {checks} checks passed.");
}
finally { await app.StopAsync(); }

sealed class FakeCustomers(Customer account) : ICustomerRepository
{
    public Task<Customer?> GetByPhoneAsync(string phone, CancellationToken ct) => Task.FromResult(phone == account.Phone ? account : null);
    public Task SaveAsync(CancellationToken ct) => Task.CompletedTask;
}
sealed class FakeStaffs(Staff account) : IStaffRepository
{
    public Task<Staff?> GetByPhoneAsync(string phone, CancellationToken ct) => Task.FromResult(phone == account.Phone ? account : null);
    public Task SaveAsync(CancellationToken ct) => Task.CompletedTask;
}
sealed class FakePets : IPetServices
{
    public long? ListOwner { get; private set; }
    public int UpdateCalls { get; private set; }
    public int CreateCalls { get; private set; }
    static PetResponse Pet(long id, long owner) => new(id, owner, "Pet", Species.DOG, null, null, null, null, PetStatus.ACTIVE, null);
    public Task<Result<PetResponse>> GetByIdAsync(long id, CancellationToken ct) => Task.FromResult(Result<PetResponse>.Success(Pet(id, 999)));
    public Task<Result<List<PetResponse>>> GetAllByIdAsync(long id, CancellationToken ct)
    { ListOwner = id; return Task.FromResult(Result<List<PetResponse>>.Success([])); }
    public Task<Result<PetResponse>> CreateAsync(CreatePetRequest req, CancellationToken ct)
    { CreateCalls++; return Task.FromResult(Result<PetResponse>.Success(Pet(78, req.CustomersId))); }
    public Task<Result<PetResponse>> UpdateAsync(long id, UpdatePetRequest req, CancellationToken ct)
    { UpdateCalls++; return Task.FromResult(Result<PetResponse>.Success(Pet(id, 999))); }
}
sealed class FakeCustomerService : ICustomerService
{
    static CustomerResponse Customer(long id) => new(id, "Customer", null, null, null, "0900000001", null, null, []);
    public Task<Result<CustomerResponse>> GetByIdAsync(long id, CancellationToken ct) => Task.FromResult(Result<CustomerResponse>.Success(Customer(id)));
    public Task<Result<CustomerResponse>> CreateAsync(CreateCustomerRequest req, CancellationToken ct) => Task.FromResult(Result<CustomerResponse>.Success(Customer(999)));
    public Task<Result<CustomerResponse>> UpdateAsync(long id, UpdateCustomerRequest req, CancellationToken ct) => Task.FromResult(Result<CustomerResponse>.Success(Customer(id)));
}

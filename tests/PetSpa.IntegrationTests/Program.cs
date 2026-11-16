// Kiểm thử các chức năng và quyền truy cập của API.
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentValidation;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.IdentityModel.Tokens;
using PetSpa.Api.Composition;
using PetSpa.Modules.Booking.Application.Abstractions;
using PetSpa.Modules.Booking.Application.Services;
using PetSpa.Modules.Booking.Domain.Entities;
using PetSpa.Modules.Booking.Endpoints;
using PetSpa.Modules.Customer.Application.Abstractions;
using PetSpa.Modules.Customer.Application.Services;
using PetSpa.Modules.Customer.Contracts;
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
using PetSpa.Modules.Resource.Application.Abstractions;
using PetSpa.Modules.Resource.Application.Services;
using PetSpa.Modules.Resource.Contracts;
using PetSpa.Modules.Resource.Domain.Entities;
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
var staff = new Staff { Id = 1, FullName = "Staff", Phone = "0900000002", Status = StaffStatus.ACTIVE };
var customerHash = customer.PasswordHash = new PasswordHasher<Customer>(Microsoft.Extensions.Options.Options.Create(
    new PasswordHasherOptions { CompatibilityMode = PasswordHasherCompatibilityMode.IdentityV2 }))
    .HashPassword(customer, password);
var staffHash = staff.PasswordHash = new PasswordHasher<Staff>(Microsoft.Extensions.Options.Options.Create(
    new PasswordHasherOptions { CompatibilityMode = PasswordHasherCompatibilityMode.IdentityV2 }))
    .HashPassword(staff, password);
var pets = new FakePets();
var customerRepository = new FakeCustomers(customer);
var staffRepository = new FakeStaffs(staff);
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
builder.Services.AddAuthenticationServices(builder.Configuration);
builder.Services.AddAuthorizationServices();
builder.Services.AddPetSpaOpenApi();
builder.Services.ConfigureHttpJsonOptions(options => options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddSingleton<ICustomerRepository>(customerRepository);
builder.Services.AddSingleton<IStaffAuthRepository>(staffRepository);
builder.Services.AddSingleton<IPetServices>(pets);
builder.Services.AddSingleton<ICustomerService>(customerService);
builder.Services.AddSingleton<IBookingRepository>(bookings);
builder.Services.AddSingleton<ICategoryRepository>(categoryRepository);
builder.Services.AddSingleton<ICustomerModule, FakeCustomerModule>();
builder.Services.AddSingleton<IResourceModule, FakeResourceModule>();
builder.Services.AddScoped<BookingService>();
builder.Services.AddScoped<CategoryService>();
builder.Services.AddScoped<CustomerLoginService>();
builder.Services.AddScoped<StaffLoginService>();
builder.Services.AddScoped<IValidator<CustomerLoginRequest>, LoginRequestValidator<CustomerLoginRequest>>();
builder.Services.AddScoped<IValidator<StaffLoginRequest>, LoginRequestValidator<StaffLoginRequest>>();
await using var app = builder.Build();
app.UseSwaggerUI();
app.UseAuthentication();
app.UseAuthorization();
CustomerAuthEndpoints.MapCustomerAuthEndpoints(app);
app.MapOperationAuthEndpoints();
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
    using var response = await Send(HttpMethod.Post, $"/api/auth/{kind}/login", body: new { phone, password });
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
        // Lấy tham chiếu schema đăng nhập trong OpenAPI.
        string LoginSchemaRef(string kind)
        {
            var login = paths.GetProperty($"/api/auth/{kind}/login").GetProperty("post");
            Check(!login.TryGetProperty("security", out var security) || security.GetArrayLength() == 0,
                kind + " login documented as public");
            return login.GetProperty("responses").GetProperty("200").GetProperty("content")
                .GetProperty("application/json").GetProperty("schema").GetProperty("$ref").GetString()!;
        }
        Check(LoginSchemaRef("customer") == LoginSchemaRef("staff"), "generated OpenAPI references one login schema");
        var roles = document.GetProperty("components").GetProperty("schemas").GetProperty("LoginResponse")
            .GetProperty("properties").GetProperty("role").GetProperty("enum").EnumerateArray().Select(x => x.GetString());
        Check(roles.SequenceEqual(new[] { "customer", "staff", "receptionist", "manager", "admin" }),
            "generated OpenAPI lists all canonical roles");
        var categories = paths.GetProperty("/api/categories");
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
    var customerToken = await Login("customer", " " + customer.Phone + " ", AccountRoles.Customer, 1);
    Check(customer.LastLoginAt.HasValue && customer.PasswordHash != customerHash && customerRepository.SaveCalls == 1,
        "customer login rehashes and records LastLoginAt");
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
    using (var other = await Send(HttpMethod.Put, "/api/customers/999", customerToken, new { fullName = "Updated" }))
        Check(other.StatusCode == HttpStatusCode.Forbidden && customerService.LastUpdatedId == 1,
            "customer cannot update another profile");
    var profileBody = new { fullName = "Created", phone = "0900000999", password };
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
    foreach (var role in Enum.GetValues<StaffRole>())
    {
        staff.Role = role;
        var token = await Login("staff", staff.Phone, role.ToString().ToLowerInvariant(), 1);
        if (role == StaffRole.STAFF)
            Check(staff.LastLoginAt.HasValue && staff.PasswordHash != staffHash && staffRepository.SaveCalls == 1,
                "staff login rehashes and records LastLoginAt");
        using (var allowed = await Send(HttpMethod.Get, "/test/staff", token))
            Check(allowed.StatusCode == HttpStatusCode.OK, role + " accepted by resource staff policy");
        using (var manager = await Send(HttpMethod.Get, "/test/manager", token))
            Check(manager.StatusCode == (role is StaffRole.MANAGER or StaffRole.ADMIN ? HttpStatusCode.OK : HttpStatusCode.Forbidden),
                role + " manager/admin RequireRole policy");
        using (var list = await Send(HttpMethod.Get, "/api/customers", token))
            Check(list.StatusCode == (role == StaffRole.STAFF ? HttpStatusCode.Forbidden : HttpStatusCode.OK),
                role + " customer list policy");
        using (var profile = await Send(HttpMethod.Get, "/api/customers/999", token))
            Check(profile.StatusCode == (role == StaffRole.STAFF ? HttpStatusCode.Forbidden : HttpStatusCode.OK),
                role + " customer management permissions");
        using (var sameId = await Send(HttpMethod.Get, "/api/customers/1", token))
            Check(sameId.StatusCode == (role == StaffRole.STAFF ? HttpStatusCode.Forbidden : HttpStatusCode.OK),
                role + " staff ID does not grant customer ownership");
        using (var profile = await Send(HttpMethod.Post, "/api/customers", token, profileBody))
            Check(profile.StatusCode == (role == StaffRole.STAFF ? HttpStatusCode.Forbidden : HttpStatusCode.Created),
                role + " create customer policy");
        using (var write = await Send(HttpMethod.Post, "/api/categories", token, categoryBody))
            Check(write.StatusCode == HttpStatusCode.Created, role + " can write to real category endpoint");
        using (var booking = await Send(HttpMethod.Get, "/api/bookings/2", token))
            Check(booking.StatusCode == HttpStatusCode.OK, role + " can read a customer's booking");
        using (var list = await Send(HttpMethod.Get, "/api/customers/999/pets", token))
            Check(list.StatusCode == (role == StaffRole.STAFF ? HttpStatusCode.Forbidden : HttpStatusCode.OK)
                && (role == StaffRole.STAFF || pets.ListOwner == 999), role + " pet list permissions and requested owner");
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
    using (var update = await Send(HttpMethod.Put, "/api/bookings/1", customerToken,
        new { customerId = 1L, petId = 20L, bookingTime = DateTime.UtcNow.AddDays(1), status = "PENDING" }))
        Check(update.StatusCode == HttpStatusCode.BadRequest && bookings.SaveCalls == 0,
            "booking rejects a pet that belongs to a different customer");
    using (var create = await Send(HttpMethod.Post, "/api/bookings", customerToken,
        new { customerId = 1L, petId = 10L, bookingTime = DateTime.UtcNow.AddDays(1), serviceOptionIds = new[] { 1L } }))
        Check(create.StatusCode == HttpStatusCode.Created && bookings.SaveCalls == 1, "customer can book for their own pet");
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
        Check(update.StatusCode == HttpStatusCode.NotFound && pets.UpdateCalls == 0, "pet update rejects another owner's pet");
    using (var create = await Send(HttpMethod.Post, "/api/customers/1/pets", customerToken,
        new { customersId = 999, name = "Not allowed", species = Species.DOG }))
        Check(create.StatusCode == HttpStatusCode.BadRequest && pets.CreateCalls == 0, "pet creation rejects mismatched body owner");
    foreach (var kind in new[] { "customer", "staff" })
    {
        var phone = kind == "customer" ? customer.Phone : staff.Phone;
        using var wrong = await Send(HttpMethod.Post, $"/api/auth/{kind}/login", body: new { phone, password = "wrong" });
        Check(wrong.StatusCode == HttpStatusCode.Unauthorized, kind + " wrong password -> 401");
        using var invalid = await Send(HttpMethod.Post, $"/api/auth/{kind}/login", body: new { phone = "", password });
        Check(invalid.StatusCode == HttpStatusCode.BadRequest, kind + " invalid input -> 400");
        using var missing = await Send(HttpMethod.Post, $"/api/auth/{kind}/login", body: new { phone = "0900000099", password });
        Check(missing.StatusCode == HttpStatusCode.Unauthorized, kind + " unknown account -> 401");
    }
    customer.Status = CustomerStatus.INACTIVE;
    staff.Status = StaffStatus.INACTIVE;
    using (var inactive = await Send(HttpMethod.Post, "/api/auth/customer/login", body: new { customer.Phone, password }))
        Check(inactive.StatusCode == HttpStatusCode.Unauthorized, "inactive customer -> 401");
    using (var inactive = await Send(HttpMethod.Post, "/api/auth/staff/login", body: new { staff.Phone, password }))
        Check(inactive.StatusCode == HttpStatusCode.Unauthorized, "inactive staff -> 401");
    staff.Status = StaffStatus.ACTIVE;
    staff.Role = (StaffRole)999;
    var lastLogin = staff.LastLoginAt;
    var saveCalls = staffRepository.SaveCalls;
    using (var invalidRole = await Send(HttpMethod.Post, "/api/auth/staff/login", body: new { phone = staff.Phone, password }))
        Check(invalidRole.StatusCode == HttpStatusCode.Unauthorized && staff.LastLoginAt == lastLogin
            && staffRepository.SaveCalls == saveCalls, "unknown staff role cannot receive a token or record a login");
    var legacyClaims = new Claim[]
    {
        new("sub", "staff:1"), new("account_type", "staff"), new("staff_id", "1"), new("account_role", "MANAGER")
    };
    using (var legacy = await Send(HttpMethod.Get, "/test/staff", IssueTestToken(legacyClaims)))
        Check(legacy.StatusCode == HttpStatusCode.Forbidden, "legacy claims cannot bypass the role policy");
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
        Check(booking.StatusCode == HttpStatusCode.Forbidden && fallback.StatusCode == HttpStatusCode.Forbidden
            && write.StatusCode == HttpStatusCode.Forbidden, description + " -> 403 on account, fallback and staff policies");
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

sealed class FakeCustomers(Customer account) : ICustomerRepository
{
    public int SaveCalls { get; private set; }
    // Tìm tài khoản theo số điện thoại.
    public Task<Customer?> GetByPhoneAsync(string phone, CancellationToken ct) => Task.FromResult(phone == account.Phone ? account : null);
    // Ghi nhận số lần lưu trong dữ liệu giả.
    public Task SaveAsync(CancellationToken ct) { SaveCalls++; return Task.CompletedTask; }
}
sealed class FakeStaffs(Staff account) : IStaffAuthRepository
{
    public int SaveCalls { get; private set; }
    // Tìm tài khoản theo số điện thoại.
    public Task<Staff?> GetByPhoneAsync(string phone, CancellationToken ct) => Task.FromResult(phone == account.Phone ? account : null);
    // Ghi nhận số lần lưu trong dữ liệu giả.
    public Task SaveAsync(CancellationToken ct) { SaveCalls++; return Task.CompletedTask; }
}
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
    { LastUpdatedId = id; return Task.FromResult(Result<CustomerResponse>.Success(Customer(id))); }
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
}

sealed class FakeResourceModule : IResourceModule
{
    // Lấy các tùy chọn dịch vụ đang hoạt động.
    public Task<IReadOnlyList<BookableServiceOption>> GetActiveServiceOptionsAsync(
        IReadOnlyCollection<long> ids, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<BookableServiceOption>>([new(1, 100_000)]);
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

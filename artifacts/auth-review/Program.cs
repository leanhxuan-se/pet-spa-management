using System.IdentityModel.Tokens.Jwt;
using System.Reflection;
using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Identity;
using PetSpa.Api.Composition;
using PetSpa.Modules.Customer;
using PetSpa.Modules.Customer.Application.Abstractions;
using PetSpa.Modules.Customer.Endpoints;
using PetSpa.Modules.Customer.Endpoints.Requests;
using PetSpa.Modules.Customer.Endpoints.Responses;
using PetSpa.Modules.Customer.Domain.Enums;
using PetSpa.Modules.Operation;
using PetSpa.Modules.Operation.Endpoints;
using PetSpa.Modules.Operation.Endpoints.Requests;
using PetSpa.SharedKernel.Application.Abstractions;

var services = new ServiceCollection();
services.AddLogging(builder => builder.ClearProviders());
services.AddCustomerModule("Host=localhost;Database=unused;Username=unused");
services.AddOperationModule("Host=localhost;Database=unused;Username=unused");
var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
{
    ["Jwt:Issuer"] = "auth-review",
    ["Jwt:Audience"] = "auth-review",
    ["Jwt:SigningKey"] = "auth-review-test-key-only-never-production-123456789",
    ["Jwt:AccessTokenMinutes"] = "15"
}).Build();
services.AddAuthenticationServices(config);
using var provider = services.BuildServiceProvider();
using var scope = provider.CreateScope();
var sp = scope.ServiceProvider;
Console.WriteLine($"Customer login validator registered: {sp.GetService<IValidator<CustomerLoginRequest>>() is not null}");
Console.WriteLine($"Staff login validator registered: {sp.GetService<IValidator<StaffLoginRequest>>() is not null}");

foreach (var type in new[] { typeof(CustomerAuthEndpoints), typeof(OperationAuthEndpoints) })
{
    var method = type.GetMethod("LoginAsync", BindingFlags.NonPublic | BindingFlags.Static)!;
    try
    {
        RequestDelegateFactory.InferMetadata(method, new RequestDelegateFactoryOptions { ServiceProvider = sp });
        Console.WriteLine($"{type.Name}: endpoint binding OK");
    }
    catch (InvalidOperationException ex)
    {
        Console.WriteLine($"{type.Name}: endpoint binding FAILED: {ex.Message}");
    }
}

var tokenService = sp.GetRequiredService<ITokenService>();
var bearer = sp.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>().Get("Bearer");
var tokenHandler = new JwtSecurityTokenHandler { MapInboundClaims = bearer.MapInboundClaims };
var customerToken = tokenService.Generate([
    new Claim("sub", "customer:42"), new Claim("account_type", "customer"), new Claim("customer_id", "42")
]);
var customerPrincipal = tokenHandler.ValidateToken(customerToken.AccessToken, bearer.TokenValidationParameters, out _);
Console.WriteLine($"Customer JWT valid: {customerPrincipal.Identity!.IsAuthenticated}");
Console.WriteLine($"Customer claim customer_id: {customerPrincipal.FindFirst("customer_id")?.Value}");
Console.WriteLine($"Customer claim customerId: {customerPrincipal.FindFirst("customerId")?.Value ?? "<missing>"}");

var recorder = new RecordingCustomerService();
var update = new UpdateCustomerRequest("Review", null, null, null, null, null, null, null);
var updateResult = await CustomerEndpoints.UpdateCustomerAsync(update, recorder, customerPrincipal, CancellationToken.None);
Console.WriteLine($"Customer update receives id: {recorder.LastId}; HTTP result: {(updateResult as IStatusCodeHttpResult)?.StatusCode}");

var staffToken = tokenService.Generate([
    new Claim("sub", "staff:42"), new Claim("account_type", "staff"),
    new Claim("staff_id", "42"), new Claim("account_role", "MANAGER")
]);
var staffPrincipal = tokenHandler.ValidateToken(staffToken.AccessToken, bearer.TokenValidationParameters, out _);
Console.WriteLine($"Staff JWT valid: {staffPrincipal.Identity!.IsAuthenticated}");
Console.WriteLine($"Staff account_role: {staffPrincipal.FindFirst("account_role")?.Value}");
Console.WriteLine($"Staff IsInRole(MANAGER): {staffPrincipal.IsInRole("MANAGER")}");

var policies = sp.GetRequiredService<IAuthorizationPolicyProvider>();
var authorization = sp.GetRequiredService<IAuthorizationService>();
var defaultPolicy = await policies.GetDefaultPolicyAsync();
Console.WriteLine($"Customer token allowed by profile default policy: {(await authorization.AuthorizeAsync(customerPrincipal, null, defaultPolicy)).Succeeded}");
Console.WriteLine($"Staff token allowed by profile default policy: {(await authorization.AuthorizeAsync(staffPrincipal, null, defaultPolicy)).Succeeded}");

var customerHasher = sp.GetRequiredService<IPasswordHasher<PetSpa.Modules.Customer.Domain.Entities.Customer>>();
var customer = new PetSpa.Modules.Customer.Domain.Entities.Customer
{
    Id = 42, Phone = "0900000000", FullName = "Review", Status = CustomerStatus.ACTIVE
};
var oldHasher = new PasswordHasher<PetSpa.Modules.Customer.Domain.Entities.Customer>(
    Options.Create(new PasswordHasherOptions { IterationCount = 1000 }));
var oldHash = oldHasher.HashPassword(customer, "Review-password-only");
customer.PasswordHash = oldHash;
var customerRepo = new ReviewCustomerRepository { Customer = customer };
var customerLogin = new PetSpa.Modules.Customer.Application.Services.CustomerLoginService(
    customerRepo, sp.GetRequiredService<PetSpa.Modules.Customer.Application.Abstractions.IPasswordService>());
Console.WriteLine($"Customer service correct password returns account: {await customerLogin.LoginAsync(customer.Phone, "Review-password-only", CancellationToken.None) is not null}");
Console.WriteLine($"Customer service rehashes and saves login time: {customer.PasswordHash != oldHash && customer.LastLoginAt.HasValue && customerRepo.Saves == 1}");
Console.WriteLine($"Customer service wrong password rejected: {await customerLogin.LoginAsync(customer.Phone, "wrong", CancellationToken.None) is null}");
customer.Status = CustomerStatus.INACTIVE;
Console.WriteLine($"Customer service inactive account rejected: {await customerLogin.LoginAsync(customer.Phone, "Review-password-only", CancellationToken.None) is null}");
customerRepo.Customer = null;
Console.WriteLine($"Customer service missing account rejected: {await customerLogin.LoginAsync(customer.Phone, "Review-password-only", CancellationToken.None) is null}");

var staffHasher = sp.GetRequiredService<IPasswordHasher<PetSpa.Modules.Operation.Domain.Entities.Staff>>();
var staffAccount = new PetSpa.Modules.Operation.Domain.Entities.Staff { Id = 42, FullName = "Review", Phone = "0900000000" };
var oldStaffHasher = new PasswordHasher<PetSpa.Modules.Operation.Domain.Entities.Staff>(
    Options.Create(new PasswordHasherOptions { IterationCount = 1000 }));
var oldStaffHash = oldStaffHasher.HashPassword(staffAccount, "Review-password-only");
staffAccount.PasswordHash = oldStaffHash;
var staffRepo = new ReviewStaffRepository { Staff = staffAccount };
var staffLogin = new PetSpa.Modules.Operation.Application.Services.StaffLoginService(
    staffRepo, sp.GetRequiredService<PetSpa.Modules.Operation.Application.Abstractions.IPasswordService>());
Console.WriteLine($"Staff service correct password returns account: {await staffLogin.LoginAsync(staffAccount.Phone, "Review-password-only", CancellationToken.None) is not null}");
Console.WriteLine($"Staff service rehashes and saves login time: {staffAccount.PasswordHash != oldStaffHash && staffAccount.LastLoginAt.HasValue && staffRepo.Saves == 1}");
Console.WriteLine($"Staff service wrong password rejected: {await staffLogin.LoginAsync(staffAccount.Phone, "wrong", CancellationToken.None) is null}");
staffAccount.Status = PetSpa.Modules.Operation.Domain.Enums.StaffStatus.INACTIVE;
Console.WriteLine($"Staff service inactive account rejected: {await staffLogin.LoginAsync(staffAccount.Phone, "Review-password-only", CancellationToken.None) is null}");
Console.WriteLine($"JWT already issued to staff still validates: {tokenHandler.ValidateToken(staffToken.AccessToken, bearer.TokenValidationParameters, out _).Identity!.IsAuthenticated}");
staffRepo.Staff = null;
Console.WriteLine($"Staff service missing account rejected: {await staffLogin.LoginAsync(staffAccount.Phone, "Review-password-only", CancellationToken.None) is null}");
Console.WriteLine($"Customer password correct: {customerHasher.VerifyHashedPassword(customer, customerHasher.HashPassword(customer, "Review-password-only"), "Review-password-only")}");
Console.WriteLine($"Customer password incorrect: {customerHasher.VerifyHashedPassword(customer, oldHash, "wrong")}");
Console.WriteLine($"Customer old hash: {customerHasher.VerifyHashedPassword(customer, oldHash, "Review-password-only")}");
try
{
    customerHasher.VerifyHashedPassword(customer, "not-a-valid-base64-hash", "Review-password-only");
    Console.WriteLine("Malformed stored hash handled without exception");
}
catch (Exception ex)
{
    Console.WriteLine($"Malformed stored hash throws: {ex.GetType().Name}");
}

// Verify that the absent staff validator prevents materializing the combined route table.
var builder = WebApplication.CreateBuilder();
builder.Logging.ClearProviders();
builder.Services.AddCustomerModule("Host=localhost;Database=unused;Username=unused");
builder.Services.AddOperationModule("Host=localhost;Database=unused;Username=unused");
builder.Services.AddAuthenticationServices(config);
await using var app = builder.Build();
app.MapCustomerModuleEndpoint();
app.MapOperationModuleEndpoint();
try
{
    var routes = ((IEndpointRouteBuilder)app).DataSources.SelectMany(source => source.Endpoints).ToList();
    Console.WriteLine($"Combined auth route table OK: {routes.Count} routes");
}
catch (InvalidOperationException ex)
{
    Console.WriteLine($"Combined auth route table FAILED: {ex.Message}");
}

internal sealed class RecordingCustomerService : ICustomerService
{
    public long LastId { get; private set; } = -1;
    public Task<Result<CustomerResponse>> GetByIdAsync(long id, CancellationToken ct)
        => Task.FromResult(Result<CustomerResponse>.NotFound("Not used"));
    public Task<Result<CustomerResponse>> CreateAsync(CreateCustomerRequest req, CancellationToken ct)
        => Task.FromResult(Result<CustomerResponse>.NotFound("Not used"));
    public Task<Result<CustomerResponse>> UpdateAsync(long id, UpdateCustomerRequest req, CancellationToken ct)
    {
        LastId = id;
        return Task.FromResult(Result<CustomerResponse>.NotFound("Review only"));
    }
}

internal sealed class ReviewCustomerRepository : PetSpa.Modules.Customer.Domain.Repositories.ICustomerRepository
{
    public PetSpa.Modules.Customer.Domain.Entities.Customer? Customer { get; set; }
    public int Saves { get; private set; }
    public Task<PetSpa.Modules.Customer.Domain.Entities.Customer?> GetByPhoneAsync(string phone, CancellationToken ct)
        => Task.FromResult(Customer);
    public Task SaveAsync(CancellationToken ct) { Saves++; return Task.CompletedTask; }
}

internal sealed class ReviewStaffRepository : PetSpa.Modules.Operation.Domain.Repositories.IStaffRepository
{
    public PetSpa.Modules.Operation.Domain.Entities.Staff? Staff { get; set; }
    public int Saves { get; private set; }
    public Task<PetSpa.Modules.Operation.Domain.Entities.Staff?> GetByPhoneAsync(string phone, CancellationToken ct)
        => Task.FromResult(Staff);
    public Task SaveAsync(CancellationToken ct) { Saves++; return Task.CompletedTask; }
}

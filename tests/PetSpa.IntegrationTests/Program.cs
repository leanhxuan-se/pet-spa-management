using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PetSpa.Modules.Resource;
using PetSpa.Modules.Resource.Application.Abstractions;
using PetSpa.Modules.Resource.Application.Services;
using PetSpa.Modules.Resource.Domain.Entities;
using PetSpa.Modules.Resource.Domain.Enums;
using PetSpa.Modules.Resource.Infrastructure.Persistence;
using PetSpa.Modules.Resource.Infrastructure.Repositories;
using PetSpa.ResourceSmokeTests;

const string Connection = "Host=localhost;Database=unused_resource_tests;Username=unused";
var checks = 0;
void Check(bool condition, string message)
{
    if (!condition)
        throw new InvalidOperationException(message);
    checks++;
}

// Production DI and EF model/migrations must be usable without connecting to a database.
var productionServices = new ServiceCollection();
productionServices.AddLogging();
productionServices.AddResourceModule(Connection);
using (var provider = productionServices.BuildServiceProvider(new ServiceProviderOptions
    { ValidateScopes = true, ValidateOnBuild = true }))
using (var scope = provider.CreateScope())
{
    Check(scope.ServiceProvider.GetRequiredService<ICategoryRepository>() is CategoryRepository, "Category DI");
    Check(scope.ServiceProvider.GetRequiredService<IServiceRepository>() is ServiceRepository, "Service DI");
    Check(scope.ServiceProvider.GetRequiredService<IServiceOptionRepository>() is ServiceOptionRepository, "Option DI");
    var categoryService = scope.ServiceProvider.GetRequiredService<CategoryService>();
    Check(ReferenceEquals(categoryService, scope.ServiceProvider.GetRequiredService<CategoryService>()), "Scoped service DI");
    scope.ServiceProvider.GetRequiredService<ServiceCatalogService>();
    scope.ServiceProvider.GetRequiredService<ServiceOptionService>();
    var db = scope.ServiceProvider.GetRequiredService<ResourceDbContext>();
    Check(!db.Database.HasPendingModelChanges(), "Migration snapshot differs from EF model");
    Check(db.Model.FindEntityType(typeof(Category))!.FindProperty(nameof(Category.Status)) is not null, "Category status mapping");
    Check(db.Model.FindEntityType(typeof(ServiceOption))!.FindProperty(nameof(ServiceOption.Status)) is not null, "Option status mapping");
    var sql = db.GetService<IMigrator>().GenerateScript();
    Check(sql.Contains("UPDATE resource.category SET status = 'ACTIVE'") &&
        sql.Contains("UPDATE resource.service_options SET status = 'ACTIVE'"), "Migration must backfill valid enum values");
}

var builder = WebApplication.CreateBuilder();
builder.WebHost.UseUrls("http://127.0.0.1:0");
builder.Logging.ClearProviders();
builder.Services.AddResourceModule(Connection);
builder.Services.AddSingleton<ResourceTestStore>();
builder.Services.AddScoped<ICategoryRepository, TestCategoryRepository>();
builder.Services.AddScoped<IServiceRepository, TestServiceRepository>();
builder.Services.AddScoped<IServiceOptionRepository, TestServiceOptionRepository>();
builder.Services.AddAuthentication("ResourceTest")
    .AddScheme<AuthenticationSchemeOptions, ResourceTestAuthentication>("ResourceTest", _ => { });
builder.Services.AddAuthorization();

await using var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();
app.MapResourceModuleEndpoint();
await app.StartAsync();
var address = app.Services.GetRequiredService<IServer>().Features
    .Get<IServerAddressesFeature>()!.Addresses.Single();
using var client = new HttpClient { BaseAddress = new Uri(address) };

async Task<JsonElement> SendAsync(
    HttpMethod method, string path, HttpStatusCode expected, object? body = null, bool createdLocation = false)
{
    using var request = new HttpRequestMessage(method, path);
    if (body is not null)
        request.Content = JsonContent.Create(body);
    using var response = await client.SendAsync(request);
    var content = await response.Content.ReadAsStringAsync();
    Check(response.StatusCode == expected, $"{method} {path}: expected {expected}, got {response.StatusCode}: {content}");
    if (createdLocation)
        Check(response.Headers.Location is not null, $"{path}: missing Location header");
    return string.IsNullOrEmpty(content) ? default : JsonDocument.Parse(content).RootElement.Clone();
}

async Task ExpectCountAsync(string path, int count)
{
    var result = await SendAsync(HttpMethod.Get, path, HttpStatusCode.OK);
    Check(result.GetArrayLength() == count, $"{path}: expected {count} records, got {result.GetArrayLength()}");
}

var categoryBody = new { name = "  Spa  ", description = "Dịch vụ chăm sóc", status = "ACTIVE" };
await ExpectCountAsync("/api/resource/categories", 0);
await SendAsync(HttpMethod.Post, "/api/resource/categories", HttpStatusCode.Unauthorized, categoryBody);
client.DefaultRequestHeaders.Add("X-Test-Account", "customer");
await SendAsync(HttpMethod.Post, "/api/resource/categories", HttpStatusCode.Forbidden, categoryBody);
client.DefaultRequestHeaders.Remove("X-Test-Account");
client.DefaultRequestHeaders.Add("X-Test-Account", "staff");

var category = await SendAsync(HttpMethod.Post, "/api/resource/categories", HttpStatusCode.Created, categoryBody, true);
var categoryId = category.GetProperty("id").GetInt64();
Check(category.GetProperty("name").GetString() == "Spa", "Category name should be trimmed");
Check(category.GetProperty("status").GetString() == "ACTIVE", "Status should serialize as string");
await SendAsync(HttpMethod.Get, $"/api/resource/categories/{categoryId}", HttpStatusCode.OK);
await ExpectCountAsync($"/api/resource/categories/{categoryId}/services", 0);
await SendAsync(HttpMethod.Post, "/api/resource/categories", HttpStatusCode.Conflict, categoryBody);
await SendAsync(HttpMethod.Post, "/api/resource/categories", HttpStatusCode.BadRequest, new { name = " " });
await SendAsync(HttpMethod.Post, "/api/resource/categories", HttpStatusCode.BadRequest, new { name = new string('a', 51) });
await SendAsync(HttpMethod.Post, "/api/resource/categories", HttpStatusCode.BadRequest, new { name = "Other", status = "DELETED" });
await SendAsync(HttpMethod.Post, "/api/resource/categories", HttpStatusCode.BadRequest, new { name = "Other", status = 99 });
await SendAsync(HttpMethod.Post, "/api/resource/categories", HttpStatusCode.BadRequest, new { name = "Other", status = "INVALID" });
await SendAsync(HttpMethod.Put, $"/api/resource/categories/{categoryId}", HttpStatusCode.OK,
    new { name = "Spa", description = "Đã cập nhật", status = "INACTIVE" });
Check(app.Services.GetRequiredService<ResourceTestStore>().Categories.Single().UpdatedAt >=
    app.Services.GetRequiredService<ResourceTestStore>().Categories.Single().CreatedAt, "Category update timestamp");
await SendAsync(HttpMethod.Put, "/api/resource/categories/999", HttpStatusCode.NotFound, new { name = "Missing" });

var otherCategory = await SendAsync(HttpMethod.Post, "/api/resource/categories", HttpStatusCode.Created, new { name = "Khác" });
var otherCategoryId = otherCategory.GetProperty("id").GetInt64();
await SendAsync(HttpMethod.Put, $"/api/resource/categories/{otherCategoryId}", HttpStatusCode.Conflict, new { name = "Spa" });

var serviceBody = new { categoryId, serviceName = "  Tắm thú cưng  ", description = "Tắm và sấy", status = "ACTIVE" };
var service = await SendAsync(HttpMethod.Post, "/api/resource/services", HttpStatusCode.Created, serviceBody, true);
var serviceId = service.GetProperty("id").GetInt64();
Check(service.GetProperty("serviceName").GetString() == "Tắm thú cưng", "Service name should be trimmed");
await SendAsync(HttpMethod.Get, $"/api/resource/services/{serviceId}", HttpStatusCode.OK);
await SendAsync(HttpMethod.Post, "/api/resource/services", HttpStatusCode.Created,
    new { categoryId = otherCategoryId, serviceName = "Cắt móng" });
await ExpectCountAsync($"/api/resource/categories/{categoryId}/services", 1);
await ExpectCountAsync($"/api/resource/categories/{otherCategoryId}/services", 1);
await ExpectCountAsync($"/api/resource/services?categoryId={categoryId}&keyword=Tắm", 1);
await ExpectCountAsync($"/api/resource/services?categoryId={categoryId}&keyword=không-có", 0);
await SendAsync(HttpMethod.Get, "/api/resource/categories/999/services", HttpStatusCode.NotFound);
await SendAsync(HttpMethod.Post, "/api/resource/services", HttpStatusCode.NotFound,
    new { categoryId = 999, serviceName = "Missing parent" });
await SendAsync(HttpMethod.Post, "/api/resource/services", HttpStatusCode.BadRequest,
    new { categoryId = 0, serviceName = "Invalid" });
await SendAsync(HttpMethod.Post, "/api/resource/services", HttpStatusCode.BadRequest,
    new { categoryId, serviceName = " " });
await SendAsync(HttpMethod.Post, "/api/resource/services", HttpStatusCode.BadRequest,
    new { categoryId, serviceName = new string('a', 257) });
await SendAsync(HttpMethod.Put, $"/api/resource/services/{serviceId}", HttpStatusCode.NotFound,
    new { categoryId = 999, serviceName = "Missing parent" });
await SendAsync(HttpMethod.Put, $"/api/resource/services/{serviceId}", HttpStatusCode.OK,
    new { categoryId, serviceName = "Tắm và sấy", status = "MAINTAINED" });

var optionBody = new
{
    serviceId, species = "DOG", minWeight = 0m, maxWeight = 5m, durationMinutes = 30, unitPrice = 150000.50m
};
var option = await SendAsync(HttpMethod.Post, "/api/resource/service-options", HttpStatusCode.Created, optionBody, true);
var optionId = option.GetProperty("id").GetInt64();
Check(option.GetProperty("species").GetString() == "DOG", "Species should serialize as string");
Check(option.GetProperty("unitPrice").GetDecimal() == 150000.50m, "Price must preserve decimal precision");
await SendAsync(HttpMethod.Get, $"/api/resource/service-options/{optionId}", HttpStatusCode.OK);
await ExpectCountAsync($"/api/resource/services/{serviceId}/options", 1);
await ExpectCountAsync($"/api/resource/service-options?serviceId={serviceId}", 1);
await SendAsync(HttpMethod.Get, "/api/resource/services/999/options", HttpStatusCode.NotFound);
await SendAsync(HttpMethod.Post, "/api/resource/service-options", HttpStatusCode.NotFound,
    new { serviceId = 999, species = "CAT", durationMinutes = 20, unitPrice = 0 });
var invalidOption = await SendAsync(HttpMethod.Post, "/api/resource/service-options", HttpStatusCode.BadRequest,
    new { serviceId, species = 99, minWeight = 10, maxWeight = 5, durationMinutes = 0, unitPrice = -1 });
var errors = invalidOption.GetProperty("errors");
foreach (var field in new[] { "Species", "MaxWeight", "DurationMinutes", "UnitPrice" })
    Check(errors.TryGetProperty(field, out _), $"Missing validation error: {field}");
await SendAsync(HttpMethod.Post, "/api/resource/service-options", HttpStatusCode.BadRequest,
    new { serviceId, species = "CAT", minWeight = -1, maxWeight = -2, durationMinutes = 30, unitPrice = 100 });
await SendAsync(HttpMethod.Post, "/api/resource/service-options", HttpStatusCode.BadRequest,
    new { serviceId, durationMinutes = 30, unitPrice = 100 });
await SendAsync(HttpMethod.Post, "/api/resource/service-options", HttpStatusCode.BadRequest,
    new { serviceId, species = "DOG", durationMinutes = 30 });
await SendAsync(HttpMethod.Put, $"/api/resource/service-options/{optionId}", HttpStatusCode.OK,
    new { serviceId, species = "CAT", minWeight = (decimal?)null, maxWeight = (decimal?)null, durationMinutes = 45, unitPrice = 0 });
await SendAsync(HttpMethod.Put, "/api/resource/service-options/999", HttpStatusCode.NotFound, optionBody);

// All three resource types enforce write authorization.
client.DefaultRequestHeaders.Remove("X-Test-Account");
foreach (var path in new[] { $"/api/resource/categories/{categoryId}", $"/api/resource/services/{serviceId}",
    $"/api/resource/service-options/{optionId}" })
    await SendAsync(HttpMethod.Delete, path, HttpStatusCode.Unauthorized);
client.DefaultRequestHeaders.Add("X-Test-Account", "staff");

await SendAsync(HttpMethod.Delete, $"/api/resource/service-options/{optionId}", HttpStatusCode.NoContent);
await SendAsync(HttpMethod.Get, $"/api/resource/service-options/{optionId}", HttpStatusCode.NotFound);
await SendAsync(HttpMethod.Delete, $"/api/resource/service-options/{optionId}", HttpStatusCode.NotFound);
await ExpectCountAsync($"/api/resource/services/{serviceId}/options", 0);
var retainedOption = await SendAsync(HttpMethod.Post, "/api/resource/service-options", HttpStatusCode.Created, optionBody);
var retainedOptionId = retainedOption.GetProperty("id").GetInt64();
await SendAsync(HttpMethod.Delete, $"/api/resource/services/{serviceId}", HttpStatusCode.NoContent);
await SendAsync(HttpMethod.Get, $"/api/resource/services/{serviceId}", HttpStatusCode.NotFound);
await SendAsync(HttpMethod.Get, $"/api/resource/service-options/{retainedOptionId}", HttpStatusCode.NotFound);
await SendAsync(HttpMethod.Get, $"/api/resource/services/{serviceId}/options", HttpStatusCode.NotFound);
await SendAsync(HttpMethod.Post, "/api/resource/service-options", HttpStatusCode.NotFound, optionBody);
await ExpectCountAsync($"/api/resource/categories/{categoryId}/services", 0);

// Deleting an ancestor must also hide otherwise undeleted children.
var child = await SendAsync(HttpMethod.Post, "/api/resource/services", HttpStatusCode.Created, serviceBody);
var childId = child.GetProperty("id").GetInt64();
var childOption = await SendAsync(HttpMethod.Post, "/api/resource/service-options", HttpStatusCode.Created,
    new { serviceId = childId, species = "DOG", durationMinutes = 20, unitPrice = 50000 });
var childOptionId = childOption.GetProperty("id").GetInt64();
await SendAsync(HttpMethod.Delete, $"/api/resource/categories/{categoryId}", HttpStatusCode.NoContent);
await SendAsync(HttpMethod.Get, $"/api/resource/categories/{categoryId}", HttpStatusCode.NotFound);
await SendAsync(HttpMethod.Get, $"/api/resource/services/{childId}", HttpStatusCode.NotFound);
await SendAsync(HttpMethod.Get, $"/api/resource/service-options/{childOptionId}", HttpStatusCode.NotFound);
await SendAsync(HttpMethod.Post, "/api/resource/services", HttpStatusCode.NotFound, serviceBody);
await SendAsync(HttpMethod.Post, "/api/resource/categories", HttpStatusCode.Conflict, categoryBody);
await ExpectCountAsync("/api/resource/categories", 1);
await ExpectCountAsync("/api/resource/services", 1);
await ExpectCountAsync("/api/resource/service-options", 0);
var store = app.Services.GetRequiredService<ResourceTestStore>();
Check(store.Categories.Count == 2 && store.Services.Count == 3 && store.Options.Count == 3,
    "Soft deletion must retain rows");
Check(store.Services.Single(s => s.Id == childId).Status != ResourceStatus.DELETED &&
    store.Options.Single(o => o.Id == childOptionId).Status != ResourceStatus.DELETED,
    "Ancestor deletion must hide descendants without changing their status");

// The application also converts a unique-name race reported by the repository to a conflict.
store.FailNextCategorySave = true;
await SendAsync(HttpMethod.Post, "/api/resource/categories", HttpStatusCode.Conflict, new { name = "Race" });

await app.StopAsync();
Console.WriteLine($"PASS: {checks} checks (production DI/EF migrations and HTTP CRUD, hierarchy, validation, authorization, soft deletion).");

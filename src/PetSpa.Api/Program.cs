// Khởi tạo ứng dụng, dịch vụ và các endpoint API.
using PetSpa.Api.Composition;
using PetSpa.Modules.Auth;
using PetSpa.Modules.Billing;
using PetSpa.Modules.Booking;
using PetSpa.Modules.Customer;
using PetSpa.Modules.Operation;
using PetSpa.Modules.Resource;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
    throw new InvalidOperationException("Set ConnectionStrings:DefaultConnection in appsettings.json or in the process environment.");

// Register Modules DbContexts
builder.Services.AddBillingModule(connectionString);
builder.Services.AddBookingModule(connectionString);
builder.Services.AddCustomerModule(connectionString);
builder.Services.AddOperationModule(connectionString);
builder.Services.AddResourceModule(connectionString);

// Register Authentication Service 
builder.Services.AddAuthModule(builder.Configuration, connectionString);

//Register CORS (cross-origin system) to allow fetching api
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyHeader()
              .AllowAnyMethod()
              .AllowAnyOrigin();
    });
});

builder.Services.AddPetSpaOpenApi();


//serializer chuyển enum sang string để trả về cho FE, tránh trả về số
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    // Adding Swagger and OpenAPI endpoints for testing API
    app.MapOpenApi().AllowAnonymous();
    app.MapOpenApi("/swagger/{documentName}/swagger.json").AllowAnonymous();
    app.UseSwaggerUI();
}

// Activate HTTPS
app.UseHttpsRedirection();

// Activate CORS
app.UseCors("AllowAll");



app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapHealthEndpoint();
app.MapAuthModuleEndpoint();

//Register EndpointMap
app.MapCustomerModuleEndpoint();
app.MapOperationModuleEndpoint();
app.MapResourceModuleEndpoint();
app.MapBookingModuleEndpoint();


app.Run();

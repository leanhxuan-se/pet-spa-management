using Microsoft.Extensions.Options;
using PetSpa.Api.Composition;
using PetSpa.Modules.Billing;
using PetSpa.Modules.Booking;
using PetSpa.Modules.Customer;
using PetSpa.Modules.Operation;
using PetSpa.Modules.Resource;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);


var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Oh my god, no database to connect, sorry 😭");

// Register Modules DbContexts
builder.Services.AddBillingModule(connectionString);
builder.Services.AddBookingModule(connectionString);
builder.Services.AddCustomerModule(connectionString);
builder.Services.AddOperationModule(connectionString);
builder.Services.AddResourceModule(connectionString);

// Register Authentication Service 
builder.Services.AddAuthenticationServices(builder.Configuration);

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

builder.Services.AddSwaggerGen(option =>
{
    option.CustomSchemaIds(type => type.FullName?.Replace("+", "."));
});


builder.Services.AddOpenApi();


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
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Activate HTTPS
app.UseHttpsRedirection();

// Activate CORS
app.UseCors("AllowAll");
app.UseCors("AllowAll");


app.UseAuthentication();
app.UseAuthorization();

//Register EndpointMap
app.MapCustomerModuleEndpoint();
app.MapOperationModuleEndpoint();
app.MapResourceModuleEndpoint();


app.Run();

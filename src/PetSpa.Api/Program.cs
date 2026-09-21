using PetSpa.Modules.Billing;
using PetSpa.Modules.Booking;
using PetSpa.Modules.Customer;
using PetSpa.Modules.Operation;
using PetSpa.Modules.Resource;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Oh my god, no database to connect, sorry 😭");

// Register Modules DbContexts
builder.Services.AddBillingModule(connectionString);
builder.Services.AddBookingModule(connectionString);
builder.Services.AddCustomerModule(connectionString);
builder.Services.AddOperationModule(connectionString);
builder.Services.AddResourceModule(connectionString);


// Register Swagger Services
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    // Adding Swagger and OpenAPI endpoints for testing API
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.Run();
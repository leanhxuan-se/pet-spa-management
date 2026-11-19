using FluentValidation;
using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using PetSpa.Modules.Customer.Application.Abstractions;
using PetSpa.Modules.Customer.Domain.Enums;
using PetSpa.Modules.Customer.Endpoints.Requests;
using PetSpa.Modules.Customer.Endpoints.Responses;
using PetSpa.Modules.Customer.Infrastructure.Persistence;
using PetSpa.Modules.Customer.Infrastructure.Persistence.Configurations;
using PetSpa.SharedKernel.Application.Abstractions;
using CustomerTable = PetSpa.Modules.Customer.Domain.Entities.Customer;

namespace PetSpa.Modules.Customer.Application.Services;

internal sealed class CustomerService(
    CustomerDbContext db,
    IServiceProvider serviceProvider,
    IPasswordService<CustomerTable> passwordService) : ICustomerService
{
    public async Task<Result<List<CustomerResponse>>> GetAsync(CancellationToken ct)
    {
        var customers = await db.Customers.AsNoTracking()
            .Where(x => x.Status == CustomerStatus.ACTIVE)
            .ProjectToType<CustomerResponse>()
            .ToListAsync(ct);

        return customers.Count == 0
            ? Result<List<CustomerResponse>>.NotFound("Customers not found")
            : Result<List<CustomerResponse>>.Success(customers);
    }

    public async Task<Result<CustomerResponse>> GetByIdAsync(long id, CancellationToken ct)
    {
        var customer = await db.Customers.AsNoTracking()
            .Where(x => x.Id == id)
            .ProjectToType<CustomerResponse>()
            .FirstOrDefaultAsync(ct);

        return customer is null
            ? Result<CustomerResponse>.NotFound("Customer not found")
            : Result<CustomerResponse>.Success(customer);
    }

    public async Task<Result<CustomerResponse>> CreateAsync(CreateCustomerRequest req, CancellationToken ct)
    {
        var validator = serviceProvider.GetRequiredService<IValidator<CreateCustomerRequest>>();
        var validation = await validator.ValidateAsync(req, ct);
        if (!validation.IsValid)
            return Result<CustomerResponse>.ValidationFailed(validation.ToDictionary());

        var customer = new CustomerTable
        {
            FullName = req.FullName.Trim(),
            Phone = CustomerContact.NormalizePhone(req.Phone),
            Email = CustomerContact.NormalizeEmail(req.Email),
            Gender = req.Gender,
            DateOfBirth = req.DateOfBirth,
            Status = CustomerStatus.ACTIVE
        };
        customer.PasswordHash = passwordService.HashPassword(customer, req.Password);
        db.Customers.Add(customer);

        return await SaveAsync(customer, ct);
    }

    public async Task<Result<CustomerResponse>> UpdateAsync(long id, UpdateCustomerRequest req, CancellationToken ct)
    {
        var validator = serviceProvider.GetRequiredService<IValidator<UpdateCustomerRequest>>();
        var validation = await validator.ValidateAsync(req, ct);
        if (!validation.IsValid)
            return Result<CustomerResponse>.ValidationFailed(validation.ToDictionary());

        var customer = await db.Customers.Include(x => x.Pets).SingleOrDefaultAsync(x => x.Id == id, ct);
        if (customer is null)
            return Result<CustomerResponse>.NotFound("Customer not found");

        customer.Gender = req.Gender ?? customer.Gender;
        customer.DateOfBirth = req.DateOfBirth ?? customer.DateOfBirth;
        customer.Status = req.Status ?? customer.Status;
        customer.FullName = req.FullName?.Trim() ?? customer.FullName;
        customer.Phone = req.Phone is null ? customer.Phone : CustomerContact.NormalizePhone(req.Phone);
        customer.AvtURL = req.AvtURL?.Trim() ?? customer.AvtURL;
        if (!string.IsNullOrWhiteSpace(req.Note))
            customer.Note = req.Note;
        if (req.HasEmail)
            customer.Email = CustomerContact.NormalizeEmail(req.Email);
        if (req.Password is not null)
            customer.PasswordHash = passwordService.HashPassword(customer, req.Password);
        customer.UpdatedAt = DateTime.UtcNow;

        return await SaveAsync(customer, ct);
    }

    private async Task<Result<CustomerResponse>> SaveAsync(CustomerTable customer, CancellationToken ct)
    {
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
            ConstraintName: CustomerConfiguration.UniquePhoneIndex or CustomerConfiguration.UniqueEmailIndex
        } postgres)
        {
            db.Entry(customer).State = EntityState.Detached;
            return postgres.ConstraintName == CustomerConfiguration.UniquePhoneIndex
                ? Result<CustomerResponse>.Duplicate("phone", "Số điện thoại đã được đăng ký.")
                : Result<CustomerResponse>.Duplicate("email", "Email đã được đăng ký.");
        }

        return Result<CustomerResponse>.Success(customer.Adapt<CustomerResponse>());
    }
}

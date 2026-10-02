using FluentValidation;
using Mapster;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PetSpa.Modules.Customer.Application.Abstractions;
using PetSpa.Modules.Customer.Endpoints.Requests;
using PetSpa.Modules.Customer.Endpoints.Responses;
using PetSpa.Modules.Customer.Infrastructure.Persistence;
using PetSpa.SharedKernel.Application.Abstractions;
using CustomerTable = PetSpa.Modules.Customer.Domain.Entities.Customer;

namespace PetSpa.Modules.Customer.Application.Services
{
    public class CustomerService(
        CustomerDbContext db,
        IServiceProvider serviceProvider) : ICustomerService
    {
        public async Task<Result<CustomerResponse>> GetByIdAsync(long id, CancellationToken ct)
        {
            var customer = await db.Customers.Where(x => x.Id == id).FirstOrDefaultAsync(ct);

            if (customer is null)
            {
                return Result<CustomerResponse>.NotFound("Customer not found");
            }

            var response = customer.Adapt<CustomerResponse>();

            return Result<CustomerResponse>.Success(response);
        }

        public async Task<Result<CustomerResponse>> CreateAsync(CreateCustomerRequest req, CancellationToken ct)
        {
            var validator = serviceProvider.GetRequiredService<IValidator<CreateCustomerRequest>>();
            var validation = await validator.ValidateAsync(req, ct);

            if(!validation.IsValid)
            {
                return Result<CustomerResponse>.ValidationFailed(validation.ToDictionary());
            }

            var customer = new CustomerTable
            {
                FullName = req.FullName.Trim(),
                Phone = req.Phone.Trim(),
                Email = string.IsNullOrWhiteSpace(req.Email) ? null : req.Email.Trim(),
                Gender = req.Gender.HasValue ? req.Gender : null,
                DateOfBirth = req.DateOfBirth.HasValue ? req.DateOfBirth : null

            };

            db.Customers.Add(customer);
            await db.SaveChangesAsync(ct);

            var response = customer.Adapt<CustomerResponse>();

            return Result<CustomerResponse>.Success(response);
        }

        public async Task<Result<CustomerResponse>> UpdateAsync(long id, UpdateCustomerRequest req, CancellationToken ct)
        {
            var validator = serviceProvider.GetRequiredService<IValidator<UpdateCustomerRequest>>();
            var validation = await validator.ValidateAsync(req, ct);

            if(!validation.IsValid)
            {
                return Result<CustomerResponse>.ValidationFailed(validation.ToDictionary());
            }

            var customer = await db.Customers.Where(x => x.Id == id).FirstOrDefaultAsync(ct);

            if (customer is null)
            {
                return Result<CustomerResponse>.NotFound("Customer not found");
            }

            customer.Gender = req.Gender ?? customer.Gender;
            customer.DateOfBirth = req.DateOfBirth ?? customer.DateOfBirth;
            customer.Status = req.Status ?? customer.Status;

            customer.FullName = string.IsNullOrWhiteSpace(req.FullName) ? customer.FullName : req.FullName.Trim();
            customer.Phone = string.IsNullOrWhiteSpace(req.Phone) ? customer.Phone : req.Phone.Trim();
            customer.Email = string.IsNullOrWhiteSpace(req.Email) ? customer.Email : req.Email.Trim();
            customer.Note = string.IsNullOrWhiteSpace(req.Note) ? customer.Note : req.Note;

            customer.UpdatedAt = DateTime.UtcNow;

            await db.SaveChangesAsync(ct);

            var response = customer.Adapt<CustomerResponse>();

            return Result<CustomerResponse>.Success(response);
        }
    }
}

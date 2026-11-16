// Xử lý nghiệp vụ và cung cấp dịch vụ CustomerService.
using FluentValidation;
using Mapster;
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
    internal class CustomerService(
        CustomerDbContext db,
        IServiceProvider serviceProvider,
        IPasswordService<CustomerTable> passwordService) : ICustomerService
    {
        // Tìm đối tượng theo mã.
        public async Task<Result<CustomerResponse>> GetByIdAsync(long id, CancellationToken ct)
        {
            // 1. Lấy thông tin khách hàng từ cơ sở dữ liệu.
            var customer = await db.Customers
                .Where(x => x.Id == id)
                .AsNoTracking()
                .ProjectToType<CustomerResponse>()
                .FirstOrDefaultAsync(ct);

            if (customer is null)
            {
                return Result<CustomerResponse>.NotFound("Customer not found");
            }


            // 2. Trả kết quả xử lý.
            return Result<CustomerResponse>.Success(customer);
        }

        // Tạo đối tượng mới.
        public async Task<Result<CustomerResponse>> CreateAsync(CreateCustomerRequest req, CancellationToken ct)
        {
            // 1. Kiểm tra dữ liệu đầu vào.
            var validator = serviceProvider.GetRequiredService<IValidator<CreateCustomerRequest>>();
            var validation = await validator.ValidateAsync(req, ct);

            if(!validation.IsValid)
            {
                return Result<CustomerResponse>.ValidationFailed(validation.ToDictionary());
            }

            // 2. Tạo thông tin khách hàng mới.
            var customer = new CustomerTable
            {
                FullName = req.FullName.Trim(),
                Phone = req.Phone.Trim(),
                Email = string.IsNullOrWhiteSpace(req.Email) ? null : req.Email.Trim(),
                Gender = req.Gender.HasValue ? req.Gender : null,
                DateOfBirth = req.DateOfBirth.HasValue ? req.DateOfBirth : null

            };

            // 3. Băm mật khẩu trước khi lưu.
            customer.PasswordHash = passwordService.HashPassword(customer, req.Password);
            // 4. Thêm dữ liệu mới để lưu.
            db.Customers.Add(customer);
            // 5. Lưu các thay đổi.
            await db.SaveChangesAsync(ct);

            // 6. Chuyển dữ liệu sang dạng trả về.
            var response = customer.Adapt<CustomerResponse>();

            // 7. Trả kết quả xử lý.
            return Result<CustomerResponse>.Success(response);
        }

        // Cập nhật đối tượng.
        public async Task<Result<CustomerResponse>> UpdateAsync(long id, UpdateCustomerRequest req, CancellationToken ct)
        {
            // 1. Kiểm tra dữ liệu đầu vào.
            var validator = serviceProvider.GetRequiredService<IValidator<UpdateCustomerRequest>>();
            var validation = await validator.ValidateAsync(req, ct);

            if(!validation.IsValid)
            {
                return Result<CustomerResponse>.ValidationFailed(validation.ToDictionary());
            }

            // 2. Lấy thông tin khách hàng từ cơ sở dữ liệu.
            var customer = await db.Customers
                .Include(x => x.Pets)
                .Where(x => x.Id == id)
                .FirstOrDefaultAsync(ct);

            if (customer is null)
            {
                return Result<CustomerResponse>.NotFound("Customer not found");
            }

            // 3. Cập nhật thông tin khách hàng.
            customer.Gender = req.Gender ?? customer.Gender;
            customer.DateOfBirth = req.DateOfBirth ?? customer.DateOfBirth;
            customer.Status = req.Status ?? customer.Status;

            customer.FullName = string.IsNullOrWhiteSpace(req.FullName) ? customer.FullName : req.FullName.Trim();
            customer.Phone = string.IsNullOrWhiteSpace(req.Phone) ? customer.Phone : req.Phone.Trim();
            customer.Email = string.IsNullOrWhiteSpace(req.Email) ? customer.Email : req.Email.Trim();
            customer.Note = string.IsNullOrWhiteSpace(req.Note) ? customer.Note : req.Note;

            // 4. Cập nhật mật khẩu nếu được cung cấp.
            if (req.Password is not null)
            {
                customer.PasswordHash = passwordService.HashPassword(customer, req.Password);
            }

            customer.UpdatedAt = DateTime.UtcNow;

            // 5. Lưu các thay đổi.
            await db.SaveChangesAsync(ct);

            // 6. Chuyển dữ liệu sang dạng trả về.
            var response = customer.Adapt<CustomerResponse>();

            // 7. Trả kết quả xử lý.
            return Result<CustomerResponse>.Success(response);
        }
    }
}

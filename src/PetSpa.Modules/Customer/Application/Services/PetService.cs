// Xử lý nghiệp vụ và cung cấp dịch vụ PetService.
using FluentValidation;
using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PetSpa.Modules.Customer.Domain.Entities;
using PetSpa.Modules.Customer.Domain.Enums;
using PetSpa.Modules.Customer.Endpoints.Requests;
using PetSpa.Modules.Customer.Endpoints.Responses;
using PetSpa.Modules.Customer.Infrastructure.Persistence;
using PetSpa.SharedKernel.Application.Abstractions;

namespace PetSpa.Modules.Customer.Application.Abstractions
{
    internal class PetServices(
        CustomerDbContext db,
        IServiceProvider serviceProvider): IPetServices
    {
        // Tìm đối tượng theo mã.
        public async Task<Result<PetResponse>> GetByIdAsync(long petId, CancellationToken ct)
        {
            // 1. Lấy thông tin thú cưng từ cơ sở dữ liệu.
            var pet = await db.Pets
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == petId, ct);
            if (pet is null)
            {
                return Result<PetResponse>.NotFound($"Pet with id {petId} not found");
            }
            // 2. Chuyển dữ liệu sang dạng trả về.
            var petResponse = pet.Adapt<PetResponse>();
            // 3. Trả kết quả xử lý.
            return Result<PetResponse>.Success(petResponse);
        }
        // Lấy danh sách theo mã liên quan.
        public async Task<Result<List<PetResponse>>> GetAllByIdAsync(long id, CancellationToken ct)
        {
            // 1. Lấy thông tin thú cưng từ cơ sở dữ liệu.
            var pets = await db.Customers.AsNoTracking()
                .Where(c => c.Id == id)
                .Select(c => c.Pets.OrderBy(p => p.Id).ToList())
                .SingleOrDefaultAsync(ct);
            if (pets is null)
                return Result<List<PetResponse>>.NotFound($"Customer with id {id} not found");

            // 2. Chuyển dữ liệu sang dạng trả về.
            var petResponse = pets.Adapt<List<PetResponse>>();

            // 3. Trả kết quả xử lý.
            return Result<List<PetResponse>>.Success(petResponse);
        }
        // Tạo đối tượng mới.
        public async Task<Result<PetResponse>> CreateAsync(CreatePetRequest req, CancellationToken ct)
        {
            // 1. Kiểm tra dữ liệu đầu vào.
            var validator = serviceProvider.GetRequiredService<IValidator<CreatePetRequest>>();
            var validationResult = validator.Validate(req);
            if (!validationResult.IsValid)
            {
                return Result<PetResponse>.ValidationFailed(validationResult.ToDictionary());
            }         
            
            // 2. Lấy thông tin khách hàng từ cơ sở dữ liệu.
            await using var transaction = await db.Database.BeginTransactionAsync(ct);
            var customer = await db.Customers.FromSql(
                    $"""SELECT * FROM customer.customer WHERE "Id" = {req.CustomersId} FOR SHARE""")
                .AsNoTracking()
                .SingleOrDefaultAsync(ct);

            if(customer is null)
            {
                return Result<PetResponse>.NotFound($"Customer with id {req.CustomersId} not found");
            }

            if (customer.Status != CustomerStatus.ACTIVE)
            {
                return Result<PetResponse>.UnActive("CustomerInactive", $"Customer with id {req.CustomersId} is inactive");
            }

            // 3. Tạo thông tin thú cưng mới.
            var pet = new Pet
            {
                CustomersId = req.CustomersId,
                Name = req.Name.Trim(),
                Species = req.Species!.Value,
                Gender = req.Gender,
                Weight = req.Weight,
                Height = req.Height,
                DateOfBirth = req.DateOfBirth,
                SpecialNote = req.SpecialNote,
                Status = req.Status
            };

            // 4. Thêm dữ liệu mới để lưu.
            db.Pets.Add(pet);
            // 5. Lưu các thay đổi.
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);

            // 6. Chuyển dữ liệu sang dạng trả về.
            var petResponse = pet.Adapt<PetResponse>();
            // 7. Trả kết quả xử lý.
            return Result<PetResponse>.Success(petResponse);
        }

        // Cập nhật đối tượng.
        public async Task<Result<PetResponse>> UpdateAsync(long petId, UpdatePetRequest req, CancellationToken ct)
        {
            // 1. Kiểm tra dữ liệu đầu vào.
            var validator = serviceProvider.GetRequiredService<IValidator<UpdatePetRequest>>();
            var validationResult = validator.Validate(req);
            if (!validationResult.IsValid)
            {
                return Result<PetResponse>.ValidationFailed(validationResult.ToDictionary());
            }

            // 2. Lấy thông tin thú cưng từ cơ sở dữ liệu.
            await using var transaction = await db.Database.BeginTransactionAsync(ct);
            var pet = await db.Pets.FromSql(
                    $"""SELECT * FROM customer.pet WHERE "Id" = {petId} FOR UPDATE""")
                .SingleOrDefaultAsync(ct);
            if (pet is null)
            {
                return Result<PetResponse>.NotFound($"Pet with id {petId} not found");
            }
            if (req.HasSpecialNote && (pet.Status == PetStatus.INACTIVE || req.Status == PetStatus.INACTIVE))
            {
                return Result<PetResponse>.UnActive("PetInactive", $"Pet with id {petId} is inactive");
            }

            // 3. Cập nhật thông tin thú cưng.
            pet.Name = req.Name?.Trim() ?? pet.Name;
            pet.Species = req.Species ?? pet.Species;
            pet.Gender = req.Gender ?? pet.Gender;
            pet.Weight = req.Weight ?? pet.Weight;
            pet.Height = req.Height ?? pet.Height;
            pet.DateOfBirth = req.DateOfBirth ?? pet.DateOfBirth;
            if (req.HasSpecialNote)
                pet.SpecialNote = req.SpecialNote;
            pet.Status = req.Status ?? pet.Status;
            pet.UpdatedAt = DateTime.UtcNow;

            // 4. Lưu các thay đổi.
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            // 5. Chuyển dữ liệu sang dạng trả về.
            var petResponse = pet.Adapt<PetResponse>();

            // 6. Trả kết quả xử lý.
            return Result<PetResponse>.Success(petResponse);

        }
    }
}

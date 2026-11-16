// Định nghĩa thao tác lưu trữ và truy vấn ServiceRepository.
using Microsoft.EntityFrameworkCore;
using Npgsql;
using PetSpa.Modules.Resource.Application.Abstractions;
using PetSpa.Modules.Resource.Domain.Entities;
using PetSpa.Modules.Resource.Infrastructure.Persistence;

namespace PetSpa.Modules.Resource.Infrastructure.Repositories;

public class ServiceRepository(ResourceDbContext db) : IServiceRepository
{
    // Lấy danh sách đối tượng.
    public async Task<IReadOnlyList<Service>> ListAsync(long? categoryId, string? keyword, CancellationToken ct)
    {
        // 1. Tạo truy vấn dữ liệu ban đầu.
        var query = db.Services.AsNoTracking();
        // 2. Lọc theo danh mục nếu được chọn.
        if (categoryId.HasValue)
            query = query.Where(s => s.CategoryId == categoryId.Value);
        // 3. Lọc theo từ khóa nếu có.
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var escaped = keyword.Trim().Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
            query = query.Where(s => EF.Functions.ILike(s.ServiceName, $"%{escaped}%", "\\"));
        }

        return await query.OrderBy(s => s.ServiceName).ThenBy(s => s.Id).ToListAsync(ct);
    }

    // Tìm đối tượng theo mã.
    public Task<Service?> GetByIdAsync(long id, CancellationToken ct)
        => db.Services.FirstOrDefaultAsync(s => s.Id == id, ct);

    // Kiểm tra tên đã tồn tại.
    public Task<bool> NameExistsAsync(long categoryId, string name, long? exceptId, CancellationToken ct)
        => db.Services.AnyAsync(s => s.CategoryId == categoryId && s.ServiceName == name &&
            (!exceptId.HasValue || s.Id != exceptId.Value), ct);

    // Thêm đối tượng mới.
    public void Add(Service service) => db.Services.Add(service);

    // Thử lưu thay đổi vào cơ sở dữ liệu.
    public async Task<bool> TrySaveChangesAsync(CancellationToken ct)
    {
        try
        {
            await db.SaveChangesAsync(ct);
            return true;
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException
            { SqlState: PostgresErrorCodes.UniqueViolation, ConstraintName: "IX_service_category_id_service_name" })
        {
            return false;
        }
    }

    // Lưu thay đổi vào cơ sở dữ liệu.
    public async Task SaveChangesAsync(CancellationToken ct) => await db.SaveChangesAsync(ct);
}

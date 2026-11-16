// Quản lý truy cập cơ sở dữ liệu qua BookingDbContext.
using Microsoft.EntityFrameworkCore;
using PetSpa.Modules.Booking.Domain.Entities;
using BookingEntity = PetSpa.Modules.Booking.Domain.Entities.Booking;

namespace PetSpa.Modules.Booking.Infrastructure.Persistence;

public class BookingDbContext : DbContext
{
    // Khởi tạo DbContext với cấu hình được cung cấp.
    public BookingDbContext(DbContextOptions<BookingDbContext> options) : base(options)
    {
    }
    public DbSet<BookingEntity> Bookings => Set<BookingEntity>();
    public DbSet<BookingDetail> BookingDetails => Set<BookingDetail>();

    // Cấu hình mô hình dữ liệu của DbContext.
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        //Add all table to booking schema to prevent table name conflicts with other modules
        modelBuilder.HasDefaultSchema("booking");
        // Apply configurations owned by this module.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BookingDbContext).Assembly);
    }
}

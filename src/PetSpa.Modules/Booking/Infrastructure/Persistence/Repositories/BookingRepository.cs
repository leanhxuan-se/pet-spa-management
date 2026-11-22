// Định nghĩa thao tác lưu trữ và truy vấn BookingRepository.
using Microsoft.EntityFrameworkCore;
using Npgsql;
using PetSpa.Modules.Booking.Application.Abstractions;
using PetSpa.Modules.Booking.Application.Dtos;
using PetSpa.Modules.Booking.Domain.Entities;
using PetSpa.Modules.Booking.Domain.Enums;
using PetSpa.Modules.Booking.Domain.Exceptions;
using BookingEntity = PetSpa.Modules.Booking.Domain.Entities.Booking;

namespace PetSpa.Modules.Booking.Infrastructure.Persistence.Repositories;

public sealed class BookingRepository(BookingDbContext db) : IBookingRepository
{
    public Task<BookingEntity?> GetByIdAsync(long id, CancellationToken ct)
        => db.Bookings.AsNoTracking().Include(b => b.BookingDetails).SingleOrDefaultAsync(b => b.Id == id, ct);

    public async Task<BookingEntity?> GetForUpdateAsync(long id, CancellationToken ct)
    {
        if (db.Database.CurrentTransaction is null)
            throw new InvalidOperationException("Booking updates require a database transaction.");

        // Lấy khóa trước khi đọc các dòng chi tiết để nhìn thấy trạng thái vừa được commit.
        var lockedIds = await db.Database.SqlQuery<long>(
            $"""SELECT id AS "Value" FROM booking.booking WHERE id = {id} FOR UPDATE""").ToListAsync(ct);
        if (lockedIds.Count == 0)
            return null;

        db.ChangeTracker.Clear();
        return await db.Bookings.Include(b => b.BookingDetails).SingleAsync(b => b.Id == id, ct);
    }

    public async Task<IReadOnlyList<BookingEntity>> ReadPageAsync(
        BookingListQuery filters, BookingCursor? after, int limit, CancellationToken ct)
    {
        var bookings = db.Bookings.AsNoTracking();
        if (filters.Date is { } day)
        {
            var midnight = day.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            bookings = bookings.Where(b => b.BookingTime >= midnight && b.BookingTime < midnight.AddDays(1));
        }
        if (filters.Status is { } status)
            bookings = bookings.Where(b => b.Status == status);
        if (filters.CustomerId is { } customerId)
            bookings = bookings.Where(b => b.CustomerId == customerId);
        if (filters.ContactPhone is { } phone)
            bookings = bookings.Where(b => b.ContactPhone == phone);

        var ordered = bookings.Select(b => new
        {
            Booking = b,
            StatusOrder = b.Status == BookingStatus.COMPLETED ? 0
                : b.Status == BookingStatus.CONFIRMED ? 1
                : b.Status == BookingStatus.PAID ? 2
                : b.Status == BookingStatus.PENDING ? 3 : 4
        });
        if (after is { } cursor)
        {
            if (cursor.BookingTime is { } time)
                ordered = ordered.Where(r => r.StatusOrder > cursor.StatusOrder
                    || r.StatusOrder == cursor.StatusOrder && (r.Booking.BookingTime == null
                        || r.Booking.BookingTime > time
                        || r.Booking.BookingTime == time && r.Booking.Id > cursor.Id));
            else
                ordered = ordered.Where(r => r.StatusOrder > cursor.StatusOrder
                    || r.StatusOrder == cursor.StatusOrder && r.Booking.BookingTime == null && r.Booking.Id > cursor.Id);
        }

        return await ordered.OrderBy(r => r.StatusOrder)
            .ThenBy(r => r.Booking.BookingTime == null)
            .ThenBy(r => r.Booking.BookingTime)
            .ThenBy(r => r.Booking.Id)
            .Take(limit)
            .Select(r => r.Booking)
            .Include(b => b.BookingDetails)
            .ToListAsync(ct);
    }

    public async Task<T> InTransactionAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var result = await action(ct);
        await transaction.CommitAsync(ct);
        return result;
    }

    public void Add(BookingEntity booking) => db.Bookings.Add(booking);
    public void RemoveDetail(BookingDetail detail) => db.BookingDetails.Remove(detail);

    public async Task SaveChangesAsync(CancellationToken ct)
    {
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException
            { SqlState: PostgresErrorCodes.ForeignKeyViolation, SchemaName: "operation", TableName: "service_session" })
        {
            throw new BookingDetailInUseException(exception);
        }
    }
}

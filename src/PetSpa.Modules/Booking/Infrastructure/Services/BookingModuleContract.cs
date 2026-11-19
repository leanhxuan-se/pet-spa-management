using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using PetSpa.Modules.Booking.Application.Abstractions;
using PetSpa.Modules.Booking.Contracts;
using PetSpa.Modules.Booking.Domain.Entities;
using PetSpa.Modules.Booking.Domain.Enums;
using PetSpa.Modules.Booking.Infrastructure.Persistence;
using PetSpa.Modules.Booking.Infrastructure.Repositories;
using BookingEntity = PetSpa.Modules.Booking.Domain.Entities.Booking;

namespace PetSpa.Modules.Booking.Infrastructure.Services;

internal sealed class BookingModuleContract(IBookingRepository reader) : IBookingModule
{
    public Task<BookingContractResult<BookingHandoffCandidate>> GetHandoffCandidateAsync(
        long bookingId, DbTransaction transaction, CancellationToken ct = default)
        => WithLockedBookingAsync(bookingId, transaction, (booking, _, _) =>
        {
            var result = CanStartHandoff(booking) || booking.HasCompleteHandoff()
                ? BookingContractResult<BookingHandoffCandidate>.Success(new(booking.Id, booking.PetId, booking.BookingTime,
                    SelectHandoffDetails(booking).OrderBy(detail => detail.Id).Select(ToSessionDetail).ToArray(),
                    booking.SessionsCreatedAt))
                : BookingContractResult<BookingHandoffCandidate>.Conflict("Lịch chưa đủ điều kiện bàn giao.");
            return Task.FromResult(result);
        }, ct);

    public Task<BookingContractResult<BookingHandoff>> MarkSessionsCreatedAsync(
        long bookingId, IReadOnlyCollection<long> bookingDetailIds, DbTransaction transaction, CancellationToken ct = default)
        => WithLockedBookingAsync(bookingId, transaction, async (booking, repository, token) =>
        {
            var selected = bookingDetailIds.ToHashSet();
            if (selected.Count == 0 || selected.Count != bookingDetailIds.Count || selected.Any(id => id <= 0))
                return BookingContractResult<BookingHandoff>.Conflict("Tập dòng bàn giao phải có ID hợp lệ và không trùng.");

            if (booking.SessionsCreatedAt.HasValue)
            {
                return booking.HasCompleteHandoff() && selected.SetEquals(booking.HandedOffDetailIds!)
                    ? BookingContractResult<BookingHandoff>.Success(ToHandoff(booking))
                    : BookingContractResult<BookingHandoff>.Conflict("Lịch đã bàn giao với tập dòng khác hoặc thiếu dữ liệu.");
            }
            if (!CanStartHandoff(booking) || !selected.SetEquals(SelectHandoffDetails(booking).Select(d => d.Id)))
                return BookingContractResult<BookingHandoff>.Conflict("Bàn giao yêu cầu lịch CONFIRMED và đủ tập dòng còn hiệu lực.");

            var now = BookingEntity.UtcNow();
            booking.SessionsCreatedAt = now;
            booking.HandedOffDetailIds = selected.Order().ToArray();
            booking.UpdatedAt = now;
            await repository.SaveChangesAsync(token);
            return BookingContractResult<BookingHandoff>.Success(ToHandoff(booking));
        }, ct);

    public Task<BookingContractResult<BookingSessionDetail>> SyncDetailStatusAsync(
        long bookingId, long bookingDetailId, BookingDetailStatus status, DbTransaction transaction,
        CancellationToken ct = default)
        => WithLockedBookingAsync(bookingId, transaction, async (booking, repository, token) =>
        {
            if (!booking.HasCompleteHandoff() || !booking.HandedOffDetailIds!.Contains(bookingDetailId))
                return BookingContractResult<BookingSessionDetail>.Conflict("Dòng dịch vụ không thuộc tập đã bàn giao của lịch.");
            if (status is not (BookingDetailStatus.WORKING or BookingDetailStatus.COMPLETED or BookingDetailStatus.CANCELLED))
                return BookingContractResult<BookingSessionDetail>.Conflict("Trạng thái đồng bộ không hợp lệ.");

            var detail = booking.BookingDetails.Single(d => d.Id == bookingDetailId);
            if (detail.Status == status)
                return BookingContractResult<BookingSessionDetail>.Success(ToSessionDetail(detail));
            if (booking.Status != BookingStatus.CONFIRMED || !CanSync(detail.Status, status))
                return BookingContractResult<BookingSessionDetail>.Conflict("Dòng dịch vụ không thể chuyển sang trạng thái yêu cầu.");

            detail.Status = status;
            booking.UpdatedAt = BookingEntity.UtcNow();
            await repository.SaveChangesAsync(token);
            return BookingContractResult<BookingSessionDetail>.Success(ToSessionDetail(detail));
        }, ct);

    public async Task<BookingContractResult<BillableBooking>> GetBillableBookingAsync(
        long bookingId, CancellationToken ct = default)
    {
        var booking = await reader.GetByIdAsync(bookingId, ct);
        return booking is null ? BookingContractResult<BillableBooking>.NotFound() : GetBillableResult(booking);
    }

    public Task<BookingContractResult<BillableBooking>> MarkPaidAsync(
        long bookingId, DbTransaction transaction, CancellationToken ct = default)
        => WithLockedBookingAsync(bookingId, transaction, async (booking, repository, token) =>
        {
            var billable = GetBillableResult(booking);
            if (billable.Status != BookingContractStatus.Success || booking.Status == BookingStatus.PAID)
                return billable;

            booking.Status = BookingStatus.PAID;
            booking.UpdatedAt = BookingEntity.UtcNow();
            await repository.SaveChangesAsync(token);
            return GetBillableResult(booking);
        }, ct);

    private static async Task<BookingContractResult<T>> WithLockedBookingAsync<T>(
        long bookingId, DbTransaction transaction,
        Func<BookingEntity, IBookingRepository, CancellationToken, Task<BookingContractResult<T>>> action,
        CancellationToken ct)
    {
        if (transaction is not NpgsqlTransaction { Connection: NpgsqlConnection connection })
            throw new ArgumentException("An active PostgreSQL transaction is required.", nameof(transaction));

        var options = new DbContextOptionsBuilder<BookingDbContext>().UseNpgsql(connection).Options;
        await using var db = new BookingDbContext(options);
        await using var enlisted = await db.Database.UseTransactionAsync(transaction, ct);
        var repository = new BookingRepository(db);
        var booking = await repository.GetForUpdateAsync(bookingId, ct);
        return booking is null ? BookingContractResult<T>.NotFound() : await action(booking, repository, ct);
    }

    private static bool CanStartHandoff(BookingEntity booking)
        => booking.Status == BookingStatus.CONFIRMED && booking.SessionsCreatedAt is null
            && !booking.LegacyHandoffNeedsReview
            && booking.BookingDetails.Any(d => d.Status == BookingDetailStatus.ASSIGNED)
            && booking.BookingDetails.All(d => d.Status is BookingDetailStatus.ASSIGNED or BookingDetailStatus.CANCELLED);

    private static IEnumerable<BookingDetail> SelectHandoffDetails(BookingEntity booking)
        => booking.HandedOffDetailIds is { } ids
            ? booking.BookingDetails.Where(d => ids.Contains(d.Id))
            : booking.BookingDetails.Where(d => d.Status == BookingDetailStatus.ASSIGNED);

    private static bool CanSync(BookingDetailStatus current, BookingDetailStatus next)
        => current == BookingDetailStatus.ASSIGNED
            || (current == BookingDetailStatus.WORKING && next == BookingDetailStatus.COMPLETED);

    private static BookingSessionDetail ToSessionDetail(BookingDetail detail)
        => new(detail.Id, detail.ServiceOptionsId, detail.Status);

    private static BookingHandoff ToHandoff(BookingEntity booking)
        => new(booking.Id, booking.SessionsCreatedAt!.Value, booking.HandedOffDetailIds!.ToArray());

    private static BookingContractResult<BillableBooking> GetBillableResult(BookingEntity booking)
    {
        if (booking.Status is not (BookingStatus.COMPLETED or BookingStatus.PAID) || !booking.HasCompleteHandoff())
            return BookingContractResult<BillableBooking>.Conflict("Lịch chưa chốt đủ các dòng đã bàn giao.");

        var details = booking.BookingDetails.Where(d => d.Status == BookingDetailStatus.COMPLETED)
            .OrderBy(d => d.Id).Select(d => new BillableBookingDetail(d.Id, d.ServiceOptionsId, d.UnitPrice)).ToArray();
        return details.Length == 0 || booking.BookingDetails.Any(d => d.Status is not (BookingDetailStatus.COMPLETED or BookingDetailStatus.CANCELLED))
            ? BookingContractResult<BillableBooking>.Conflict("Lịch không có đủ kết quả dịch vụ để lập hóa đơn.")
            : BookingContractResult<BillableBooking>.Success(new(booking.Id, booking.CustomerId, booking.Status, details));
    }
}

using System.Data.Common;
using PetSpa.Modules.Booking.Domain.Enums;

namespace PetSpa.Modules.Booking.Contracts;

public interface IBookingModule
{
    // Operation giữ khóa lịch bằng giao dịch này trước khi tạo nhóm phiên.
    Task<BookingContractResult<BookingHandoffCandidate>> GetHandoffCandidateAsync(
        long bookingId, DbTransaction transaction, CancellationToken ct = default);

    Task<BookingContractResult<BookingHandoff>> MarkSessionsCreatedAsync(
        long bookingId, IReadOnlyCollection<long> bookingDetailIds, DbTransaction transaction, CancellationToken ct = default);

    Task<BookingContractResult<BookingSessionDetail>> SyncDetailStatusAsync(
        long bookingId, long bookingDetailId, BookingDetailStatus status, DbTransaction transaction,
        CancellationToken ct = default);

    Task<IReadOnlyList<BookingSessionGroup>> GetCurrentSessionGroupsAsync(long afterBookingId, int limit,
        CancellationToken ct);

    Task<BookingContractResult<BillableBooking>> GetBillableBookingAsync(long bookingId, CancellationToken ct = default);

    // Billing gọi cùng giao dịch thu tiền. Bên gọi chịu trách nhiệm commit hoặc rollback.
    Task<BookingContractResult<BillableBooking>> MarkPaidAsync(
        long bookingId, DbTransaction transaction, CancellationToken ct = default);
}

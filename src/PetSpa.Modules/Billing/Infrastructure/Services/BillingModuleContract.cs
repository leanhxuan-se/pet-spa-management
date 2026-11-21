using Microsoft.EntityFrameworkCore;
using PetSpa.Modules.Billing.Contracts;
using PetSpa.Modules.Billing.Domain.Entities;
using PetSpa.Modules.Billing.Domain.Enums;
using PetSpa.Modules.Billing.Infrastructure.Persistence;

namespace PetSpa.Modules.Billing.Infrastructure.Services;

internal sealed class BillingModuleContract(BillingDbContext db) : IBillingModule
{
    public async Task<IReadOnlyList<long>> GetBookingIdsWithActiveInvoicesAsync(long[] bookingIds, CancellationToken ct)
        => await db.Set<Invoice>().AsNoTracking()
            .Where(invoice => bookingIds.Contains(invoice.BookingId) && invoice.Status != InvoiceStatus.CANCELLED)
            .Select(invoice => invoice.BookingId)
            .Distinct()
            .ToListAsync(ct);
}

// Xử lý nghiệp vụ và cung cấp dịch vụ CustomerModuleContract.
using Microsoft.EntityFrameworkCore;
using PetSpa.Modules.Customer.Contracts;
using PetSpa.Modules.Customer.Contracts.Dtos;
using PetSpa.Modules.Customer.Domain.Entities;
using PetSpa.Modules.Customer.Domain.Enums;
using PetSpa.Modules.Customer.Infrastructure.Persistence;

namespace PetSpa.Modules.Customer.Infrastructure.Services;

internal sealed class CustomerModuleContract(CustomerDbContext db) : ICustomerModule
{
    public Task<bool> PetBelongsToCustomerAsync(long petId, long customerId, CancellationToken ct)
        => FindActivePair(customerId, petId).AnyAsync(ct);

    public Task<BookingCustomerDto?> GetBookingCustomerAsync(
        long customerId, long petId, CancellationToken cancellationToken = default)
        => FindActivePair(customerId, petId)
            .Select(p => new BookingCustomerDto(
                p.CustomersId, p.Id, p.Customer.FullName, p.Customer.Phone, p.Species.ToString(), p.Weight))
            .SingleOrDefaultAsync(cancellationToken);

    public Task<BookingContactDto?> GetBookingContactAsync(long customerId, CancellationToken ct = default)
        => db.Customers.AsNoTracking()
            .Where(c => c.Id == customerId)
            .Select(c => new BookingContactDto(c.Id, c.FullName, c.Phone))
            .SingleOrDefaultAsync(ct);

    private IQueryable<Pet> FindActivePair(long customerId, long petId)
        => db.Pets.AsNoTracking().Where(p => p.Id == petId && p.CustomersId == customerId &&
            p.Status == PetStatus.ACTIVE && p.Customer.Status == CustomerStatus.ACTIVE);
}

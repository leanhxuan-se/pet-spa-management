using Microsoft.EntityFrameworkCore;
using CustomerRecord = PetSpa.Modules.Customer.Domain.Entities.Customer;
using PetSpa.Modules.Customer.Domain.Enums;
using PetSpa.Modules.Customer.Infrastructure.Persistence;
using PetSpa.SharedKernel.Application.Authentication;

namespace PetSpa.Modules.Customer.Infrastructure.Services;

internal sealed class CustomerAuthProfiles(CustomerDbContext db) : ICustomerAuthProfiles
{
    public async Task<AccountProfile?> FindForRegistrationByPhoneAsync(string normalizedPhone, CancellationToken ct)
    {
        var international = "+84" + normalizedPhone[1..];
        var matches = await Profiles(db.Customers
            .Where(x => x.Phone.Trim() == normalizedPhone || x.Phone.Trim() == international))
            .Take(2).ToListAsync(ct);
        return matches.Count == 1 ? matches[0] : null;
    }

    public Task<AccountProfile?> GetCustomerForAuthAsync(long customerId, CancellationToken ct) =>
        Profiles(db.Customers.Where(x => x.Id == customerId)).SingleOrDefaultAsync(ct);

    private static IQueryable<AccountProfile> Profiles(IQueryable<CustomerRecord> query) => query.AsNoTracking().Select(x =>
        new AccountProfile(x.Id, x.FullName, x.Phone, x.Status == CustomerStatus.ACTIVE, AccountRoles.Customer));
}

using PetSpa.Modules.Customer.Domain.Entities;
using PetSpa.Modules.Customer.Domain.Enums;

namespace PetSpa.Modules.Customer.Endpoints.Requests
{
    public record UpdateCustomerRequest(
        string? FullName = null,
        Gender? Gender = null,
        DateOnly? DateOfBirth = null,
        string? Email = null,
        string? Phone = null,
        string? AvtURL = null,
        string? Note = null,
        CustomerStatus? Status = null,
        string? Password = null
    );
}

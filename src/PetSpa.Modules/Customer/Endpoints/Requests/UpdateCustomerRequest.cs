using PetSpa.Modules.Customer.Domain.Entities;
using PetSpa.Modules.Customer.Domain.Enums;

namespace PetSpa.Modules.Customer.Endpoints.Requests
{
    public record UpdateCustomerRequest(
        string? FullName,
        Gender? Gender,
        DateOnly? DateOfBirth,
        string? Email,
        string? Phone,
        string? AvtURL,
        string? Note,
        CustomerStatus? Status
    );
}

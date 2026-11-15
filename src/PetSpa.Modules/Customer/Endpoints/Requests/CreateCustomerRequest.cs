using PetSpa.Modules.Customer.Domain.Enums;

namespace PetSpa.Modules.Customer.Endpoints.Requests
{
    public record CreateCustomerRequest(
        string FullName,
        string Phone,
        string Password,
        Gender? Gender = null,
        DateOnly? DateOfBirth = null,
        string? Email = null,
        CustomerStatus Status = CustomerStatus.ACTIVE
   );
}

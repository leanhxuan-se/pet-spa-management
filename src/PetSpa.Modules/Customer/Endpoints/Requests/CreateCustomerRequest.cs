using PetSpa.Modules.Customer.Domain.Enums;

namespace PetSpa.Modules.Customer.Endpoints.Requests
{
    public record CreateCustomerRequest(
        string FullName,
        Gender? Gender,
        string Phone,
        DateOnly? DateOfBirth,
        string? Email,
        CustomerStatus Status = CustomerStatus.ACTIVE
   );
}

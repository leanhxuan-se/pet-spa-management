using PetSpa.Modules.Customer.Domain.Enums;

namespace PetSpa.Modules.Customer.Endpoints.Responses
{
    public record CustomerResponse(
         long Id,
         string FullName,
         Gender? Gender,
         DateOnly? DateOfBirth,
         string? Email,
         string Phone,
         string? AvtURL,
         string? Note,
         ICollection<PetResponse>? Pets,
         CustomerStatus Status = CustomerStatus.ACTIVE
    );
}

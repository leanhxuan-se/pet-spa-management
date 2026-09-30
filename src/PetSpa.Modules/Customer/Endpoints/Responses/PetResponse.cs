using PetSpa.Modules.Customer.Domain.Enums;

namespace PetSpa.Modules.Customer.Endpoints.Responses
{
    public record PetResponse
    (

        long Id,
        string Name,
        Species Species,
        double? Weight,
        int? Height,
        Gender Gender,
        DateOnly? DateOfBirth,
        PetStatus Status,
        string? SpecialNote
    );
}

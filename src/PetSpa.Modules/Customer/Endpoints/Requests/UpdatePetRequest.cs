using PetSpa.Modules.Customer.Domain.Enums;

namespace PetSpa.Modules.Customer.Endpoints.Requests
{
    public record UpdatePetRequest(
        string? Name,
        Species? Species,
        Gender? Gender,
        decimal? Weight,
        decimal? Height,
        DateOnly? DateOfBirth,
        string? SpecialNote,
        PetStatus? Status
    );
}

using PetSpa.Modules.Resource.Domain.Enums;

namespace PetSpa.Modules.Resource.Endpoints.Responses;

public record ServiceOptionResponse(
    long Id, long ServiceId, Species Species, decimal? MinWeight, decimal? MaxWeight,
    int DurationMinutes, decimal UnitPrice, ResourceStatus Status);

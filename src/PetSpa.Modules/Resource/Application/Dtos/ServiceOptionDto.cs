using PetSpa.Modules.Resource.Domain.Enums;

namespace PetSpa.Modules.Resource.Application.Dtos;

public record ServiceOptionDto(
    long Id,
    long ServiceId,
    Species Species,
    decimal? MinWeight,
    decimal? MaxWeight,
    int DurationMinutes,
    decimal UnitPrice,
    ResourceStatus Status);

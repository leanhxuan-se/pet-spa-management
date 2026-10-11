using System.Text.Json.Serialization;
using PetSpa.Modules.Resource.Domain.Enums;

namespace PetSpa.Modules.Resource.Endpoints.Requests;

public record SaveServiceOptionRequest(
    long ServiceId,
    [property: JsonRequired] Species Species,
    decimal? MinWeight,
    decimal? MaxWeight,
    int DurationMinutes,
    [property: JsonRequired] decimal UnitPrice,
    ResourceStatus Status = ResourceStatus.ACTIVE);

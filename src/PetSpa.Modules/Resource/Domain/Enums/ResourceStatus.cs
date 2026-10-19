using System.Text.Json.Serialization;

namespace PetSpa.Modules.Resource.Domain.Enums;

[JsonConverter(typeof(JsonStringEnumConverter<ResourceStatus>))]
public enum ResourceStatus
{
    ACTIVE = 0,
    INACTIVE = 3
}

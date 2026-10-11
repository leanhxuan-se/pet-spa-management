using System.Text.Json.Serialization;

namespace PetSpa.Modules.Resource.Domain.Enums;

[JsonConverter(typeof(JsonStringEnumConverter<ResourceStatus>))]
public enum ResourceStatus
{
    ACTIVE,
    MAINTAINED,
    DELETED,
    INACTIVE
}

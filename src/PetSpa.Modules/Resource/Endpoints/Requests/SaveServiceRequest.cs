using PetSpa.Modules.Resource.Domain.Enums;

namespace PetSpa.Modules.Resource.Endpoints.Requests;

public record SaveServiceRequest(
    long CategoryId, string ServiceName, string? Description = null, ResourceStatus Status = ResourceStatus.ACTIVE);

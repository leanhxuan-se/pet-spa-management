using PetSpa.Modules.Resource.Domain.Enums;

namespace PetSpa.Modules.Resource.Endpoints.Responses;

public record ServiceResponse(long Id, long CategoryId, string ServiceName, string? Description, ResourceStatus Status);

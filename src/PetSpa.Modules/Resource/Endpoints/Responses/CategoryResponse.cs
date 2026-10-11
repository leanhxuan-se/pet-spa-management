using PetSpa.Modules.Resource.Domain.Enums;

namespace PetSpa.Modules.Resource.Endpoints.Responses;

public record CategoryResponse(long Id, string Name, string? Description, ResourceStatus Status);

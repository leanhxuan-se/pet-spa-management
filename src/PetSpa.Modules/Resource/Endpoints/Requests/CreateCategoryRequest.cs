using PetSpa.Modules.Resource.Domain.Enums;

namespace PetSpa.Modules.Resource.Endpoints.Requests;

public record CreateCategoryRequest(
    string Name, string? Description = null, ResourceStatus Status = ResourceStatus.ACTIVE);

using PetSpa.Modules.Resource.Domain.Enums;

namespace PetSpa.Modules.Resource.Application.Dtos;

public record CategoryDto(long Id, string Name, string? Description, ResourceStatus Status);

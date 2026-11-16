// Định nghĩa dữ liệu trao đổi ServiceDto.
using PetSpa.Modules.Resource.Domain.Enums;

namespace PetSpa.Modules.Resource.Application.Dtos;

public record ServiceDto(long Id, long CategoryId, string ServiceName, string? Description, ResourceStatus Status);

using PetSpa.Modules.Customer.Domain.Enums;

namespace PetSpa.Modules.Customer.Contracts.Dtos;

public record PetDto
{
    public long Id { get; init; }
    public long CustomerId { get; init; }
    public string Name { get; init; } = null!;
    public string Species { get; init; } = null!;
    public double? Weight { get; init; }
    public double? Height { get; init; }
    public Gender? Gender { get; init; }
    public DateOnly? DateOfBirth { get; init; }
    public PetStatus Status { get; init; } = default!;
    public string? SpecialNote { get; init; }
}
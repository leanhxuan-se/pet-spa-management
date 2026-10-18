using PetSpa.Modules.Customer.Domain.Enums;
using PetSpa.Modules.Customer.Endpoints.Responses;

namespace PetSpa.Modules.Customer.Contracts.Dtos;

public record CustomerDto
{
    public long Id { get; init; }
    public string FullName { get; init; } = string.Empty;
    public string? Gender { get; init; }
    public DateOnly? DateOfBirth { get; init; }
    public string? Email { get; init; }
    public string? Phone { get; init; }
    public string? AvtURL { get; init; }
    public string? Note { get; init; }
    public CustomerStatus Status { get; init; } = CustomerStatus.ACTIVE;

    public IReadOnlyList<PetDto>? Pets { get; init; } = [];


}
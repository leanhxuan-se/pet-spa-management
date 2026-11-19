namespace PetSpa.Modules.Customer.Contracts.Dtos;

public sealed record BookingContactDto(long CustomerId, string FullName, string Phone);

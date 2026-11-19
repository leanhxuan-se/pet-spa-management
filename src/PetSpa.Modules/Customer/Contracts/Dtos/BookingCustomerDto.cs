namespace PetSpa.Modules.Customer.Contracts.Dtos;

public sealed record BookingCustomerDto(
    long CustomerId,
    long PetId,
    string FullName,
    string Phone,
    string Species,
    decimal? WeightKg);

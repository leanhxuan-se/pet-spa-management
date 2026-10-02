namespace PetSpa.Modules.Customer.Endpoints.Requests;

public record CustomerLoginRequest(
    string Phone,
    string Password
);
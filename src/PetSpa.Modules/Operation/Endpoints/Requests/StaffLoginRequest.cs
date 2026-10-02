namespace PetSpa.Modules.Operation.Endpoints.Requests;

public record StaffLoginRequest(
    string Phone,
    string Password
);
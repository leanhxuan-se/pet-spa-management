namespace PetSpa.SharedKernel.Application.Abstractions;

public interface ILoginRequest
{
    string Phone { get; }
    string Password { get; }
}

// Định nghĩa hợp đồng hoặc kết quả dùng chung: ILoginRequest.
namespace PetSpa.SharedKernel.Application.Abstractions;

public interface ILoginRequest
{
    string Phone { get; }
    string Password { get; }
}

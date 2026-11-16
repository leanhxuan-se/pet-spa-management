// Định nghĩa hợp đồng hoặc kết quả dùng chung: ITokenService.
using System.Security.Claims;
using PetSpa.SharedKernel.Application.Dtos;

namespace PetSpa.SharedKernel.Application.Abstractions;

public interface ITokenService
{
    // Tạo token từ các claim.
    TokenResult Generate(IEnumerable<Claim> claims);
}
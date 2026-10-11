using System.Security.Claims;
using PetSpa.SharedKernel.Application.Dtos;

namespace PetSpa.SharedKernel.Application.Abstractions;

public interface ITokenService
{
    TokenResult Generate(IEnumerable<Claim> claims);
}
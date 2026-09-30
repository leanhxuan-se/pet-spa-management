using PetSpa.Modules.Customer.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace PetSpa.Modules.Customer.Endpoints.Responses
{
    public record CustomerResponse(
         long Id,
         string FullName,
         Gender? Gender,
         DateOnly? DateOfBirth,
         string? Email,
         string Phone,
         string? AvtURL,
         string? Note,
         CustomerStatus Status = CustomerStatus.ACTIVE
    );
}

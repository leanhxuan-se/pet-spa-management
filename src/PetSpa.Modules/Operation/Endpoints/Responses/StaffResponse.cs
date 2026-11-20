using PetSpa.Modules.Operation.Domain.Entities;
using PetSpa.Modules.Operation.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace PetSpa.Modules.Operation.Endpoints.Responses
{
    internal record StaffResponse(
         long Id,
         string FullName,
         Gender? Gender,
         DateOnly? DateOfBirth,
         string Email,
         string Phone,
         string Position,
         StaffRole Role,
         DateOnly? HiredDate,
         string? AvatarUrl,
         string? Note,
         StaffStatus Status
    );
}

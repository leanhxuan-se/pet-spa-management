using PetSpa.Modules.Operation.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace PetSpa.Modules.Operation.Endpoints.Requests
{
    internal record UpdateStaffRequest(
         string? FullName,
         Gender? Gender,
         DateOnly? DateOfBirth,
         string? Password,
         string? Email,
         string? Phone,
         string? Position,
         StaffRole? Role,
         DateOnly? HiredDate,
         string? AvatarUrl,
         string? Note,
         StaffStatus? Status
        );
}

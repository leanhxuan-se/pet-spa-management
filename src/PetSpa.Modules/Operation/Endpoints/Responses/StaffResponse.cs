using System;
using System.Collections.Generic;
using System.Text;

namespace PetSpa.Modules.Operation.Endpoints.Responses
{
    internal record StaffResponse(
        long StaffId,
        string Name,
        string Position,
        string? AvtUrl
    );
}

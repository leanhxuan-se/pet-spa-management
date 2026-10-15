using PetSpa.Modules.Operation.Endpoints.Responses;
using System;
using System.Collections.Generic;
using System.Text;

namespace PetSpa.Modules.Operation.Application.Abstractions
{
    internal interface IStaffRepository
    {
        Task<List<StaffResponse>> GetAvailableAsync(DateTime startTime, DateTime endTime, CancellationToken ct);
    }
}

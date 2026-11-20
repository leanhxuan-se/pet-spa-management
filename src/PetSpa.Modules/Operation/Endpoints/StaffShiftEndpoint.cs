using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using PetSpa.Modules.Operation.Application.Services;
using PetSpa.Modules.Operation.Endpoints.Requests;

namespace PetSpa.Modules.Operation.Endpoints
{
    internal static class StaffShiftEndpoint
    {
        public static IEndpointRouteBuilder MapStaffShiftEndpoint(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/api/staff-shifts").WithTags("StaffShift");

            group.MapGet("", GetStaffShiftsAsync).WithName("GetStaffShifts").RequireAuthorization();
            group.MapPost("", CreateStaffShiftAsync).WithName("CreateStaffShift").RequireAuthorization();
            group.MapPut("/{id:long}", UpdateStaffShiftAsync).WithName("UpdateStaffShift").RequireAuthorization();
            group.MapDelete("/{id:long}", CancelStaffShiftAsync).WithName("CancelStaffShift").RequireAuthorization();
            group.MapPost("/{id:long}/start", StartStaffShiftAsync).WithName("StartStaffShift").RequireAuthorization();
            group.MapPost("/{id:long}/finish", FinishStaffShiftAsync).WithName("FinishStaffShift").RequireAuthorization();

            return app;
        }

        private static async Task<IResult> GetStaffShiftsAsync(
            [FromQuery] DateOnly? date,
            [FromQuery] long? staffId,
            [FromQuery] string? status,
            [FromQuery] string? cursor,
            [FromQuery] int? pageSize,
            ClaimsPrincipal currentUser,
            StaffShiftService service,
            CancellationToken ct)
            => (await service.GetPageAsync(date, staffId, status, cursor, pageSize, currentUser, ct)).ToHttpResult();

        private static async Task<IResult> CreateStaffShiftAsync(
            [FromBody] CreateStaffShiftRequest request,
            ClaimsPrincipal currentUser,
            StaffShiftService service,
            CancellationToken ct)
            => (await service.CreateAsync(request, currentUser, ct)).ToHttpResult();

        private static async Task<IResult> UpdateStaffShiftAsync(
            [FromRoute] long id,
            [FromBody] UpdateStaffShiftRequest request,
            ClaimsPrincipal currentUser,
            StaffShiftService service,
            CancellationToken ct)
            => (await service.UpdateAsync(id, request, currentUser, ct)).ToHttpResult();

        private static async Task<IResult> CancelStaffShiftAsync(
            [FromRoute] long id,
            ClaimsPrincipal currentUser,
            StaffShiftService service,
            CancellationToken ct)
            => (await service.CancelAsync(id, currentUser, ct)).ToHttpResult();

        private static async Task<IResult> StartStaffShiftAsync(
            [FromRoute] long id,
            ClaimsPrincipal currentUser,
            StaffShiftService service,
            CancellationToken ct)
            => (await service.StartAsync(id, currentUser, ct)).ToHttpResult();

        private static async Task<IResult> FinishStaffShiftAsync(
            [FromRoute] long id,
            ClaimsPrincipal currentUser,
            StaffShiftService service,
            CancellationToken ct)
            => (await service.FinishAsync(id, currentUser, ct)).ToHttpResult();
    }
}

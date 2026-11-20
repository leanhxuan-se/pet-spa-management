using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using PetSpa.Modules.Operation.Application.Services;
using System.Security.Claims;
using PetSpa.Modules.Operation.Endpoints.Requests;


namespace PetSpa.Modules.Operation.Endpoints
{
    internal static class StaffEndpoint
    {
        public static IEndpointRouteBuilder MapStaffEndpoint(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/api/staff").WithTags("Staff"); // gom nhóm các endpoint liên quan đến staff lại ở swagger

            group.MapGet("/availability", GetAvailableStaffAsync).WithName("GetAvailableStaffs").RequireAuthorization();
            group.MapGet("/unavailability", GetUnAvailableStaffAsync).WithName("GetUnavailableStaffs").RequireAuthorization();

            return app;
        }
        public async static Task<IResult> GetStaffsAsync(
            ClaimsPrincipal currentUser,
            StaffService service,
            CancellationToken ct)
        => (await service.GetStaffsAsync(currentUser, ct)).ToHttpResult();

        public async static Task<IResult> GetStaffByIdAsync(
            [FromRoute] long id,
            ClaimsPrincipal currentUser,
            StaffService service,
            CancellationToken ct)
        => (await service.GetStaffByIdAsync(id, currentUser, ct)).ToHttpResult();

        public async static Task<IResult> CreateStaffAsync(
            [FromBody] CreateStaffRequest request,
            ClaimsPrincipal currentUser,
            StaffService service,
            CancellationToken ct)
        => (await service.CreateStaffAsync(request, currentUser, ct)).ToHttpResult();

        public async static Task<IResult> UpdateStaffAsync(
            [FromRoute] long id,
            [FromBody] UpdateStaffRequest request,
            ClaimsPrincipal currentUser,
            StaffService service,
            CancellationToken ct)
        => (await service.UpdateStaffAsync(id, request, currentUser, ct)).ToHttpResult();

        public async static Task<IResult> DeleteStaffAsync(
            [FromRoute] long id,
            ClaimsPrincipal currentUser,
            StaffService service,
            CancellationToken ct)
        => (await service.DeleteStaffAsync(id, currentUser, ct)).ToHttpResult();

        public async static Task<IResult> GetAvailableStaffAsync(
            [FromQuery] DateTime startTime,
            [FromQuery] DateTime endTime,
            ClaimsPrincipal currentUser,
            StaffService service,
            CancellationToken ct)
        => (await service.GetAvailableStaffsAsync(startTime, endTime, currentUser, ct)).ToHttpResult();
        public async static Task<IResult> GetUnAvailableStaffAsync(
            [FromQuery] DateTime startTime,
            [FromQuery] DateTime endTime,
            ClaimsPrincipal currentUser,
            StaffService service,
            CancellationToken ct)
        => (await service.GetUnAvailableStaffsAsync(startTime, endTime, currentUser, ct)).ToHttpResult();
    }
}

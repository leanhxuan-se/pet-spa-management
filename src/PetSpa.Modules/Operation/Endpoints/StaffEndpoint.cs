using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using PetSpa.Modules.Operation.Application.Services;
using PetSpa.Modules.Operation.Endpoints.Requests;

namespace PetSpa.Modules.Operation.Endpoints
{
    internal static class StaffEndpoint
    {
        public static IEndpointRouteBuilder MapStaffEndpoint(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/api/staff").WithTags("Staff");

            group.MapGet("", GetStaffsAsync).WithName("GetStaffs").RequireAuthorization();
            group.MapGet("/{id:long}", GetStaffByIdAsync).WithName("GetStaffById").RequireAuthorization();
            group.MapPost("", CreateStaffAsync).WithName("CreateStaff").RequireAuthorization();
            group.MapPut("/{id:long}", UpdateStaffAsync).WithName("UpdateStaff").RequireAuthorization();
            group.MapDelete("/{id:long}", DeleteStaffAsync).WithName("DeleteStaff").RequireAuthorization();
            group.MapGet("/available", GetAvailableStaffsAsync).WithName("GetStaffsAvailable").RequireAuthorization();
            group.MapGet("/unavailable", GetUnavailableStaffsAsync).WithName("GetStaffsUnavailable").RequireAuthorization();

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



        private static async Task<IResult> GetAvailableStaffsAsync(
            [FromQuery] DateTime? startTime,
            [FromQuery] DateTime? endTime,
            ClaimsPrincipal currentUser,
            StaffService service,
            CancellationToken ct)
        => (await service.GetAvailableStaffsByQueryAsync(startTime, endTime, currentUser, ct)).ToHttpResult();

        private static async Task<IResult> GetUnavailableStaffsAsync(
            [FromQuery] DateTime? startTime,
            [FromQuery] DateTime? endTime,
            ClaimsPrincipal currentUser,
            StaffService service,
            CancellationToken ct)
        => (await service.GetUnavailableStaffsByQueryAsync(startTime, endTime, currentUser, ct)).ToHttpResult();
    }
}

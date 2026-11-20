using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using PetSpa.Modules.Operation.Application.Services;
using PetSpa.Modules.Operation.Endpoints.Requests;

namespace PetSpa.Modules.Operation.Endpoints
{
    internal static class RoomEndpoint
    {
        public static IEndpointRouteBuilder MapRoomEndpoint(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/api/room").WithTags("Room");

            group.MapGet("", GetRoomsAsync).WithName("GetRooms").RequireAuthorization();
            group.MapPost("", CreateRoomAsync).WithName("CreateRoom").RequireAuthorization();
            group.MapPut("/{id:long}", UpdateRoomAsync).WithName("UpdateRoom").RequireAuthorization();
            group.MapGet("/available", GetAvailableRoomsAsync).WithName("GetRoomsAvailable").RequireAuthorization();
            group.MapGet("/unavailable", GetUnavailableRoomsAsync).WithName("GetRoomsUnavailable").RequireAuthorization();

            return app;
        }

        public async static Task<IResult> GetRoomsAsync(
            RoomService service,
            ClaimsPrincipal currentUser,
            CancellationToken ct)
        => (await service.GetAllRoom(currentUser, ct)).ToHttpResult();

        public async static Task<IResult> CreateRoomAsync(
            CreateRoomRequest req,
            RoomService service,
            ClaimsPrincipal currentUser,
            CancellationToken ct)
        => (await service.CreateRoom(req, currentUser, ct)).ToHttpResult();

        public async static Task<IResult> UpdateRoomAsync(
            long id,
            UpdateRoomRequest req,
            RoomService service,
            ClaimsPrincipal currentUser,
            CancellationToken ct)
        => (await service.UpdateRoom(id, req, currentUser, ct)).ToHttpResult();

       

        private static async Task<IResult> GetAvailableRoomsAsync(
            [FromQuery] DateTime? startTime,
            [FromQuery] DateTime? endTime,
            ClaimsPrincipal currentUser,
            RoomService service,
            CancellationToken ct)
        => (await service.GetAvailableRoomsAsync(startTime, endTime, currentUser, ct)).ToHttpResult();

        private static async Task<IResult> GetUnavailableRoomsAsync(
            [FromQuery] DateTime? startTime,
            [FromQuery] DateTime? endTime,
            ClaimsPrincipal currentUser,
            RoomService service,
            CancellationToken ct)
        => (await service.GetUnavailableRoomsAsync(startTime, endTime, currentUser, ct)).ToHttpResult();
    }
}

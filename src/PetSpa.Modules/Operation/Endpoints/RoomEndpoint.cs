using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using PetSpa.Modules.Customer.Endpoints;
using PetSpa.Modules.Operation.Application.Services;
using System.Security.Claims;
using PetSpa.Modules.Operation.Endpoints.Requests;


namespace PetSpa.Modules.Operation.Endpoints
{
    internal static class RoomEndpoint
    {
        public static IEndpointRouteBuilder MapRoomEndpoint(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/api/room").WithTags("Room"); // gom nhóm các endpoint liên quan đến room lại ở swagger

            group.MapGet("", GetRoomsAsync).WithName("GetRooms").RequireAuthorization();
            group.MapPost("", CreateRoomAsync).WithName("CreateRoom").RequireAuthorization();
            group.MapPut("/{id:long}", UpdateRoomAsync).WithName("UpdateRoom").RequireAuthorization();
            group.MapGet("/availability", GetAvailableRoomAsync).WithName("GetAvailableRoom").RequireAuthorization();
            group.MapGet("/unavailability", GetUnAvailableRoomAsync).WithName("GetUnAvailableRoom").RequireAuthorization();

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
        public async static Task<IResult> GetAvailableRoomAsync(
            [FromQuery] DateTime startTime,
            [FromQuery] DateTime endTime,
            ClaimsPrincipal currentUser,
            RoomService service,
            CancellationToken ct)
        => (await service.AvailableRoom(startTime, endTime, currentUser, ct)).ToHttpResult();

        public async static Task<IResult> GetUnAvailableRoomAsync(
            [FromQuery] DateTime startTime,
            [FromQuery] DateTime endTime,
            ClaimsPrincipal currentUser,
            RoomService service,
            CancellationToken ct)
        => (await service.UnAvailableRoom(startTime, endTime, currentUser, ct)).ToHttpResult();
    }
}

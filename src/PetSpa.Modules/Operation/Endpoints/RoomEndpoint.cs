using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using PetSpa.Modules.Customer.Endpoints;
using PetSpa.Modules.Operation.Application.Services;



namespace PetSpa.Modules.Operation.Endpoints
{
    internal static class RoomEndpoint
    {
        public static IEndpointRouteBuilder MapRoomEndpoint(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/api/room").WithTags("Room"); // gom nhóm các endpoint liên quan đến room lại ở swagger

            group.MapGet("/availability", GetAvailableRoomAsync).WithName("GetAvailableRoom").RequireAuthorization();
            
            return app;
        }

        public async static Task<IResult> GetAvailableRoomAsync(
            [FromQuery] DateTime startTime,
            [FromQuery] DateTime endTime,
            RoomService service)
        => (await service.AvailableRoom(startTime, endTime)).ToHttpResult();
    }
}

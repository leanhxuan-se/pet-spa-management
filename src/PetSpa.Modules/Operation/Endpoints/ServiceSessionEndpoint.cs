using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using PetSpa.Modules.Customer.Endpoints;
using PetSpa.Modules.Operation.Application.Services;



//namespace PetSpa.Modules.Operation.Endpoints
//{
//    internal static class ServiceSessionEndpoint
//    {
//        public static IEndpointRouteBuilder MapServiceSessionEndpoint(this IEndpointRouteBuilder app)
//        {
//            var group = app.MapGroup("/api/serviceSession").WithTags("ServiceSession"); // gom nhóm các endpoint liên quan đến room lại ở swagger

//            group.MapPost("/create", CreateServiceSessionAsync).WithName("createServiceSession").RequireAuthorization();

//            return app;
//        }

//        public async static Task<IResult> CreateServiceSessionAsync(

//            RoomService service,
//            CancellationToken ct)
//        => (await service.).ToHttpResult();
//    }
//}

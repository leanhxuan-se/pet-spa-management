using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using PetSpa.Modules.Customer.Endpoints;
using PetSpa.Modules.Operation.Application.Services;
using PetSpa.Modules.Operation.Endpoints.Requests;
using System.Security.Claims;



namespace PetSpa.Modules.Operation.Endpoints
{
    internal static class ServiceSessionEndpoint
    {
        public static IEndpointRouteBuilder MapServiceSessionEndpoint(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/api/serviceSession").WithTags("ServiceSession"); // gom nhóm các endpoint liên quan đến room lại ở swagger

            group.MapPost("/create", CreateServiceSessionAsync).WithName("createServiceSession").RequireAuthorization();

            return app;
        }

        public async static Task<IResult> CreateServiceSessionAsync(
            CreateServiceSessionRequest req,
            ServiceSessionService service,
            ClaimsPrincipal currentUser,
            CancellationToken ct)
        => (await service.CreateAsync(req, currentUser?.FindFirst("account_role")?.Value!, ct)).ToHttpResult();
    }
}

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using PetSpa.Modules.Customer.Endpoints;
using PetSpa.Modules.Operation.Application.Services;



namespace PetSpa.Modules.Operation.Endpoints
{
    internal static class StaffEndpoint
    {
        public static IEndpointRouteBuilder MapStaffEndpoint(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/api/staff").WithTags("Staff"); // gom nhóm các endpoint liên quan đến room lại ở swagger

            group.MapGet("/availability", GetAvailableStaffAsync).WithName("GetAvailableStaffs").RequireAuthorization();

            return app;
        }

        public async static Task<IResult> GetAvailableStaffAsync(
            [FromQuery] DateTime startTime,
            [FromQuery] DateTime endTime,
            StaffService service,
            CancellationToken ct)
        => (await service.GetAvailableStaffsAsync(startTime, endTime, ct)).ToHttpResult();
    }
}

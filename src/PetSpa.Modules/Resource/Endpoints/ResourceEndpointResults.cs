using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using PetSpa.Modules.Resource.Application.Dtos;

namespace PetSpa.Modules.Resource.Endpoints;

internal static class ResourceEndpointResults
{
    public static IResult ToHttpResult<T>(this ResourceResult<T> result, Func<T, IResult> onSuccess)
        => result.Status switch
        {
            ResourceResultStatus.Success => onSuccess(result.Value!),
            ResourceResultStatus.NotFound => Results.NotFound(new { result.Message }),
            ResourceResultStatus.ValidationFailed => Results.ValidationProblem(result.Errors!),
            ResourceResultStatus.Conflict => Results.Conflict(new { result.Message }),
            _ => throw new InvalidOperationException("Unsupported resource result.")
        };

    public static RouteHandlerBuilder RequireStaff(this RouteHandlerBuilder builder)
        => builder.RequireAuthorization(policy =>
            policy.RequireAuthenticatedUser().RequireClaim("account_type", "staff"));
}

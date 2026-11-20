using Microsoft.AspNetCore.Http;
using PetSpa.SharedKernel.Application.Abstractions;

namespace PetSpa.Modules.Operation.Endpoints
{
    internal static class OperationResultExtensions
    {
        public static IResult ToHttpResult<T>(
            this Result<T> result,
            string? routeName = null,
            Func<T, object>? routeValues = null)
        {
            return result.Status switch
            {
                ResultStatus.Success => Results.Ok(result.Value),
                ResultStatus.Created => routeName is not null && routeValues is not null && result.Value is not null
                    ? Results.CreatedAtRoute(routeName, routeValues(result.Value), result.Value)
                    : Results.Created((string?)null, result.Value),
                ResultStatus.NoContent => Results.NoContent(),
                ResultStatus.ValidationFailed => Results.ValidationProblem(result.Errors ?? new Dictionary<string, string[]>()),
                ResultStatus.NotFound => Results.NotFound(result.Errors),
                ResultStatus.Forbidden => Results.Json(result.Errors, statusCode: StatusCodes.Status403Forbidden),
                ResultStatus.Conflict => Results.Conflict(result.Errors),
                ResultStatus.Duplicate => Results.Conflict(result.Errors),
                ResultStatus.UnActive => Results.BadRequest(result.Errors),
                ResultStatus.TimeError => Results.BadRequest(result.Errors),
                ResultStatus.BadRequest => Results.BadRequest(result.Errors),
                _ => Results.BadRequest(result.Errors)
            };
        }
    }
}

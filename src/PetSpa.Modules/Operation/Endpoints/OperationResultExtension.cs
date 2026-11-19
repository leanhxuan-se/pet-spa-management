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
                ResultStatus.Created => Results.Created(),
                ResultStatus.ValidationFailed => Results.ValidationProblem(result?.Errors!),
                ResultStatus.NotFound => Results.NotFound(result?.Errors),
                ResultStatus.UnActive => Results.BadRequest(result?.Errors),
                ResultStatus.TimeError => Results.BadRequest(result.Errors),
                ResultStatus.BadRequest => Results.BadRequest(result.Errors),
                _ => Results.BadRequest(result?.Errors)
            };
        }
    }
}

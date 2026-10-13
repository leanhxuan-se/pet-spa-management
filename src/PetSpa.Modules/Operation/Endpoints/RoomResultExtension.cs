using Microsoft.AspNetCore.Http;
using PetSpa.SharedKernel.Application.Abstractions;
using System;
using System.Collections.Generic;
using System.Text;

namespace PetSpa.Modules.Customer.Endpoints
{
    internal static class RoomResultExtensions
    {
        public static IResult ToHttpResult<T>(
            this Result<T> result,
            string? routeName = null,
            Func<T, object>? routeValues = null)
        {
            return result.Status switch
            {
                ResultStatus.Success => routeName != null && routeValues != null
                    ? Results.CreatedAtRoute(routeName, routeValues(result.Value!), result.Value)
                    : Results.Ok(result.Value),

                ResultStatus.ValidationFailed => Results.ValidationProblem(result?.Errors),
                ResultStatus.NotFound => Results.NotFound(result?.Errors),
                ResultStatus.UnActive => Results.BadRequest(result?.Errors),
                ResultStatus.TimeError => Results.BadRequest(result.Errors),
                _ => Results.BadRequest(result?.Errors)
            };
        }
    }
}

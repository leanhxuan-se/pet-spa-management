// Định nghĩa hoặc hỗ trợ các endpoint ResourceEndpointResults.
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using PetSpa.Modules.Resource.Application.Dtos;
using PetSpa.SharedKernel.Application.Authentication;

namespace PetSpa.Modules.Resource.Endpoints;

internal static class ResourceEndpointResults
{
    // Chuyển kết quả xử lý thành phản hồi HTTP.
    public static IResult ToHttpResult<T>(this ResourceResult<T> result, Func<T, IResult> onSuccess)
        => result.Status switch
        {
            ResourceResultStatus.Success => onSuccess(result.Value!),
            ResourceResultStatus.NotFound => Results.NotFound(new { result.Message }),
            ResourceResultStatus.ValidationFailed => Results.ValidationProblem(result.Errors!),
            ResourceResultStatus.Conflict => Results.Conflict(new { result.Message }),
            _ => throw new InvalidOperationException("Unsupported resource result.")
        };

    // Yêu cầu tài khoản nhân viên.
    public static RouteHandlerBuilder RequireStaff(this RouteHandlerBuilder builder)
        => builder.RequireAuthorization(AuthorizationPolicies.StaffOnly);
}

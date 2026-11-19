using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using PetSpa.Modules.Booking.Application.Dtos;
using PetSpa.Modules.Booking.Application.Services;
using PetSpa.Modules.Booking.Domain.Enums;
using PetSpa.Modules.Booking.Endpoints.Requests;
using PetSpa.SharedKernel.Application.Authentication;

namespace PetSpa.Modules.Booking.Endpoints;

public static class BookingEndpoints
{
    public static IEndpointRouteBuilder MapBookingEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/bookings").WithTags("Bookings");
        group.MapGet("/{id:long}", GetByIdAsync).WithName("GetBookingById")
            .RequireAuthorization(AuthorizationPolicies.AuthenticatedAccount)
            .Produces<BookingDto>().Produces(StatusCodes.Status404NotFound);

        var frontDesk = group.MapGroup(string.Empty).RequireAuthorization(AuthorizationPolicies.FrontDesk);
        frontDesk.MapGet("/", GetPageAsync).WithName("ListBookings")
            .Produces<BookingPageDto>().ProducesValidationProblem();
        frontDesk.MapPost("/", CreateAsync).WithName("CreateBooking")
            .Produces<BookingDto>(StatusCodes.Status201Created).ProducesValidationProblem();
        frontDesk.MapPut("/{id:long}/services", ReplaceServicesAsync).WithName("ReplaceBookingServices")
            .Produces<BookingDto>().ProducesValidationProblem()
            .Produces(StatusCodes.Status404NotFound).Produces(StatusCodes.Status409Conflict);
        frontDesk.MapPut("/{id:long}", UpdateAsync).WithName("UpdateBooking")
            .Produces<BookingDto>().ProducesValidationProblem()
            .Produces(StatusCodes.Status404NotFound).Produces(StatusCodes.Status409Conflict);
        frontDesk.MapDelete("/{id:long}", CancelAsync).WithName("CancelBooking")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound).Produces(StatusCodes.Status409Conflict);
        app.MapPatch("/api/booking/{id:long}", ChangeStatusAsync).WithName("ChangeBookingStatus")
            .WithTags("Bookings").RequireAuthorization(AuthorizationPolicies.FrontDesk)
            .Produces<BookingDto>().ProducesValidationProblem()
            .Produces(StatusCodes.Status404NotFound).Produces(StatusCodes.Status409Conflict);
        return app;
    }

    private static async Task<IResult> GetByIdAsync(
        long id, BookingService service, ClaimsPrincipal user, CancellationToken ct)
        => ToHttpResult(await service.GetByIdAsync(id, Actor(user), ct), Results.Ok);

    private static async Task<IResult> GetPageAsync(DateOnly? date, string? status, long? customerId,
        string? contactPhone, string? cursor, int? pageSize, BookingService service, ClaimsPrincipal user,
        CancellationToken ct)
    {
        BookingStatus? bookingStatus = null;
        if (status is not null)
        {
            if (!Enum.TryParse<BookingStatus>(status, ignoreCase: true, out var parsed) || !Enum.IsDefined(parsed) ||
                !string.Equals(parsed.ToString(), status, StringComparison.OrdinalIgnoreCase))
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    [nameof(status)] = ["Trạng thái lịch hẹn không hợp lệ."]
                });
            }

            bookingStatus = parsed;
        }

        var query = new BookingListQuery(date, bookingStatus, customerId, contactPhone, cursor, pageSize);
        return ToHttpResult(await service.GetPageAsync(query, Actor(user), ct), Results.Ok);
    }

    private static async Task<IResult> CreateAsync(
        CreateBookingCommand command, BookingService service, ClaimsPrincipal user, CancellationToken ct)
        => ToHttpResult(await service.CreateAsync(command, Actor(user), ct),
            dto => Results.CreatedAtRoute("GetBookingById", new { id = dto.Id }, dto));

    private static async Task<IResult> ReplaceServicesAsync(
        long id, ReplaceBookingServicesCommand command, BookingService service, ClaimsPrincipal user, CancellationToken ct)
        => ToHttpResult(await service.ReplaceServicesAsync(id, command, Actor(user), ct), Results.Ok);

    private static async Task<IResult> UpdateAsync(
        long id, UpdateBookingCommand command, BookingService service, ClaimsPrincipal user, CancellationToken ct)
        => ToHttpResult(await service.UpdateAsync(id, command, Actor(user), ct), Results.Ok);

    private static async Task<IResult> CancelAsync(
        long id, BookingService service, ClaimsPrincipal user, CancellationToken ct)
        => ToHttpResult(await service.CancelAsync(id, Actor(user), ct), _ => Results.NoContent());

    private static async Task<IResult> ChangeStatusAsync(
        long id, ChangeBookingStatusRequest request, BookingService service, ClaimsPrincipal user, CancellationToken ct)
    {
        var status = request.Status?.ToLowerInvariant() switch
        {
            "confirmed" => BookingStatus.CONFIRMED,
            "completed" => BookingStatus.COMPLETED,
            _ => (BookingStatus?)null
        };
        if (status is null)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["status"] = ["Trạng thái phải là confirmed hoặc completed."]
            });
        }

        var command = new ChangeBookingStatusCommand(status.Value);
        return ToHttpResult(await service.ChangeStatusAsync(id, command, Actor(user), ct), Results.Ok);
    }

    private static BookingActor Actor(ClaimsPrincipal user)
        => new(user.HasFrontDeskRole(), user.GetCustomerId());

    private static IResult ToHttpResult<T>(BookingResult<T> result, Func<T, IResult> onSuccess)
        => result.Status switch
        {
            BookingResultStatus.Success => onSuccess(result.Value!),
            BookingResultStatus.NotFound => Results.NotFound(new { result.Message }),
            BookingResultStatus.ValidationFailed => Results.ValidationProblem(result.Errors!),
            BookingResultStatus.Conflict => Results.Conflict(new { result.Message }),
            BookingResultStatus.Forbidden => Results.Forbid(),
            _ => throw new InvalidOperationException("Unsupported booking result.")
        };
}

using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using PetSpa.Modules.Booking.Application.Dtos;
using PetSpa.Modules.Booking.Application.Services;

namespace PetSpa.Modules.Booking.Endpoints;

public static class BookingEndpoints
{
    public static IEndpointRouteBuilder MapBookingEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/bookings").WithTags("Bookings").RequireAuthorization();
        group.MapGet("/{id:long}", GetByIdAsync).WithName("GetBookingById").Produces<BookingDto>();
        group.MapPost("/", CreateAsync).WithName("CreateBooking")
            .Produces<BookingDto>(StatusCodes.Status201Created).ProducesValidationProblem();
        group.MapPut("/{id:long}/services", ReplaceServicesAsync).WithName("ReplaceBookingServices")
            .Produces<BookingDto>().ProducesValidationProblem();
        group.MapPut("/{id:long}", UpdateAsync).WithName("UpdateBooking")
            .Produces<BookingDto>().ProducesValidationProblem();
        return app;
    }

    private static async Task<IResult> GetByIdAsync(
        long id, BookingService service, ClaimsPrincipal user, CancellationToken ct)
        => ToHttpResult(await service.GetByIdAsync(id, Actor(user), ct), Results.Ok);

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

    private static BookingActor Actor(ClaimsPrincipal user)
        => new(user.HasClaim("account_type", "staff"),
            user.HasClaim("account_type", "customer") && long.TryParse(user.FindFirst("customer_id")?.Value, out var id) && id > 0
                ? id : null);

    private static IResult ToHttpResult(BookingResult<BookingDto> result, Func<BookingDto, IResult> onSuccess)
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

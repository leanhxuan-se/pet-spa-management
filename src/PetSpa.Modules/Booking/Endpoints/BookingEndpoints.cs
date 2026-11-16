// Định nghĩa hoặc hỗ trợ các endpoint BookingEndpoints.
using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using PetSpa.Modules.Booking.Application.Dtos;
using PetSpa.Modules.Booking.Application.Services;
using PetSpa.SharedKernel.Application.Authentication;

namespace PetSpa.Modules.Booking.Endpoints;

public static class BookingEndpoints
{
    // Đăng ký endpoint lịch hẹn.
    public static IEndpointRouteBuilder MapBookingEndpoints(this IEndpointRouteBuilder app)
    {
        // 1. Tạo nhóm endpoint và cấu hình quyền truy cập.
        var group = app.MapGroup("/api/bookings").WithTags("Bookings")
            .RequireAuthorization(AuthorizationPolicies.AuthenticatedAccount);
        // 2. Đăng ký các đường dẫn và phương thức HTTP.
        group.MapGet("/{id:long}", GetByIdAsync).WithName("GetBookingById").Produces<BookingDto>();
        group.MapPost("/", CreateAsync).WithName("CreateBooking")
            .Produces<BookingDto>(StatusCodes.Status201Created).ProducesValidationProblem();
        group.MapPut("/{id:long}/services", ReplaceServicesAsync).WithName("ReplaceBookingServices")
            .Produces<BookingDto>().ProducesValidationProblem();
        group.MapPut("/{id:long}", UpdateAsync).WithName("UpdateBooking")
            .Produces<BookingDto>().ProducesValidationProblem();
        return app;
    }

    // Tìm đối tượng theo mã.
    private static async Task<IResult> GetByIdAsync(
        long id, BookingService service, ClaimsPrincipal user, CancellationToken ct)
        => ToHttpResult(await service.GetByIdAsync(id, Actor(user), ct), Results.Ok);

    // Tạo đối tượng mới.
    private static async Task<IResult> CreateAsync(
        CreateBookingCommand command, BookingService service, ClaimsPrincipal user, CancellationToken ct)
        => ToHttpResult(await service.CreateAsync(command, Actor(user), ct),
            dto => Results.CreatedAtRoute("GetBookingById", new { id = dto.Id }, dto));

    // Thay danh sách dịch vụ của lịch hẹn.
    private static async Task<IResult> ReplaceServicesAsync(
        long id, ReplaceBookingServicesCommand command, BookingService service, ClaimsPrincipal user, CancellationToken ct)
        => ToHttpResult(await service.ReplaceServicesAsync(id, command, Actor(user), ct), Results.Ok);

    // Cập nhật đối tượng.
    private static async Task<IResult> UpdateAsync(
        long id, UpdateBookingCommand command, BookingService service, ClaimsPrincipal user, CancellationToken ct)
        => ToHttpResult(await service.UpdateAsync(id, command, Actor(user), ct), Results.Ok);

    // Lấy thông tin tài khoản thực hiện yêu cầu.
    private static BookingActor Actor(ClaimsPrincipal user)
        => new(user.HasStaffRole(), user.GetCustomerId());

    // Chuyển kết quả xử lý thành phản hồi HTTP.
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

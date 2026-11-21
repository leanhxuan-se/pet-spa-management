using PetSpa.Modules.Booking.Domain.Enums;

namespace PetSpa.Modules.Operation.Endpoints.Responses;

internal sealed record ServiceSessionGroupResponse(
    long BookingId, DateTime SessionsCreatedAt, IReadOnlyList<ServiceSessionResponse> Sessions);

internal sealed record CurrentServiceSessionPage(
    IReadOnlyList<CurrentServiceSessionGroup> Items, long? NextAfterBookingId);

internal sealed record CurrentServiceSessionGroup(
    long BookingId,
    BookingStatus BookingStatus,
    DateTime? SessionsCreatedAt,
    int Waiting,
    int InProgress,
    int Completed,
    int Cancelled,
    bool CanCompleteBooking,
    bool CanCancelBooking,
    bool CanCreateInvoice,
    IReadOnlyList<ServiceSessionResponse> Sessions);

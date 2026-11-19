// Định nghĩa dữ liệu trao đổi BookingCommands.
using System.Text.Json.Serialization;
using PetSpa.Modules.Booking.Domain.Enums;

namespace PetSpa.Modules.Booking.Application.Dtos;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record CreateBookingCommand(
    long CustomerId, long PetId, DateTime BookingTime, string? Note,
    IReadOnlyList<long>? ServiceOptionIds);

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class UpdateBookingCommand
{
    [JsonIgnore]
    public bool HasBookingTime { get; private set; }

    [JsonIgnore]
    public bool HasContactName { get; private set; }

    [JsonIgnore]
    public bool HasContactPhone { get; private set; }

    [JsonIgnore]
    public bool HasNote { get; private set; }

    public DateTime? BookingTime
    {
        get;
        init { field = value; HasBookingTime = true; }
    }

    public string? ContactName
    {
        get;
        init { field = value; HasContactName = true; }
    }

    public string? ContactPhone
    {
        get;
        init { field = value; HasContactPhone = true; }
    }

    public string? Note
    {
        get;
        init { field = value; HasNote = true; }
    }
}

public sealed record ChangeBookingStatusCommand(BookingStatus Status);

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record ReplaceBookingServicesCommand(IReadOnlyList<long>? ServiceOptionIds);

public sealed record BookingListQuery(
    DateOnly? Date, BookingStatus? Status, long? CustomerId, string? ContactPhone, string? Cursor, int? PageSize);

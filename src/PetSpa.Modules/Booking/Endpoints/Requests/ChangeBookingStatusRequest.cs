using System.Text.Json.Serialization;

namespace PetSpa.Modules.Booking.Endpoints.Requests;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record ChangeBookingStatusRequest([property: JsonRequired] string Status);

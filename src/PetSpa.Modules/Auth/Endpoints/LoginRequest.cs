using System.Text.Json.Serialization;

namespace PetSpa.Modules.Auth.Endpoints;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
internal sealed record LoginRequest(string? Phone, string? Password);

using System.Text.Json.Serialization;

namespace PetSpa.Modules.Customer.Endpoints.Requests;

public sealed record UpdatePetSpecialNoteRequest([property: JsonRequired] string? SpecialNote);

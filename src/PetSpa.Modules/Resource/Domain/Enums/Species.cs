namespace PetSpa.Modules.Resource.Domain.Enums;

[System.Text.Json.Serialization.JsonConverter(typeof(System.Text.Json.Serialization.JsonStringEnumConverter<Species>))]
public enum Species
{
    DOG,
    CAT,
    CHICKEN,
    FISH,
    OTHER
}

namespace PetSpa.Modules.Auth.Infrastructure.Tokens;

internal sealed class RefreshToken
{
    public Guid Id { get; set; }
    public required string UserId { get; set; }
    public Guid FamilyId { get; set; }
    public required byte[] TokenHash { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset ExpiresAtUtc { get; set; }
    public DateTimeOffset? RevokedAtUtc { get; set; }
    public byte[]? ReplacedByTokenHash { get; set; }
}

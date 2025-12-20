namespace SoftielRemote.Backend.Api.Domain;

public sealed class RefreshToken
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public string Token { get; init; } = default!;
    public DateTime CreatedAt { get; init; }
    public DateTime ExpiresAt { get; init; }
    public bool IsRevoked { get; init; }
}


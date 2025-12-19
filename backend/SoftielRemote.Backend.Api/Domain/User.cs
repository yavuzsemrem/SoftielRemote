namespace SoftielRemote.Backend.Api.Domain;

public sealed class User
{
    public Guid Id { get; init; }
    public string Email { get; init; } = default!;
    public string PasswordHash { get; init; } = default!;
    public string Role { get; init; } = "user";
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
}

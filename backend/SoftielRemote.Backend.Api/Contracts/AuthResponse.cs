namespace SoftielRemote.Backend.Api.Contracts;

public sealed record AuthResponse(string AccessToken, string RefreshToken, int ExpiresIn);

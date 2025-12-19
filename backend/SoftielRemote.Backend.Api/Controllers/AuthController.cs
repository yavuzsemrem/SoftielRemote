using BCrypt.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SoftielRemote.Backend.Api.Contracts;
using SoftielRemote.Backend.Api.Data;
using SoftielRemote.Backend.Api.Services;

namespace SoftielRemote.Backend.Api.Controllers;

[ApiController]
[Route("auth")]
public sealed class AuthController : ControllerBase
{
    private readonly AuthRepository _repo;
    private readonly JwtTokenService _tokens;

    public AuthController(AuthRepository repo, JwtTokenService tokens)
    {
        _repo = repo;
        _tokens = tokens;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] AuthRegisterRequest req)
    {
        var email = req.Email.Trim();

        var exists = await _repo.GetUserByEmail(email);
        if (exists != null) return Conflict(new { error = "email_already_exists" });

        Guid userId;
        try
        {
            userId = await _repo.CreateUser(email, req.Password);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Email already exists"))
        {
            return Conflict(new { error = "email_already_exists" });
        }

        // issue tokens (default role is "user")
        var access = _tokens.CreateAccessToken(userId, email, "user");
        var refresh = _tokens.CreateRefreshToken();
        var expiresAt = _tokens.RefreshTokenExpiresAtUtc().DateTime;

        await _repo.InsertRefreshToken(userId, refresh, expiresAt);

        return Ok(new AuthResponse(access, refresh, _tokens.GetAccessTokenExpiresInSeconds()));
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] AuthLoginRequest req)
    {
        var email = req.Email.Trim();

        var user = await _repo.GetUserByEmail(email);
        if (user == null) return Unauthorized(new { error = "invalid_credentials" });

        if (!user.IsActive) return Unauthorized(new { error = "account_inactive" });

        var ok = await _repo.VerifyPassword(user, req.Password);
        if (!ok) return Unauthorized(new { error = "invalid_credentials" });

        var access = _tokens.CreateAccessToken(user.Id, user.Email, user.Role);
        var refresh = _tokens.CreateRefreshToken();
        var expiresAt = _tokens.RefreshTokenExpiresAtUtc().DateTime;

        await _repo.InsertRefreshToken(user.Id, refresh, expiresAt);

        return Ok(new AuthResponse(access, refresh, _tokens.GetAccessTokenExpiresInSeconds()));
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] AuthRefreshRequest req)
    {
        var token = req.RefreshToken?.Trim();
        if (string.IsNullOrWhiteSpace(token))
            return BadRequest(new { error = "refresh_missing" });

        var stored = await _repo.GetRefreshToken(token);
        if (stored == null) return Unauthorized(new { error = "refresh_invalid" });
        
        // Reuse detection: revoked token kullanılırsa tüm session'ları revoke et
        if (stored.IsRevoked)
        {
            await _repo.RevokeAllUserTokens(stored.UserId);
            return Unauthorized(new { error = "refresh_revoked" });
        }
        
        if (stored.ExpiresAt <= DateTime.UtcNow) return Unauthorized(new { error = "refresh_expired" });

        var user = await _repo.GetUserById(stored.UserId);
        if (user == null) return Unauthorized(new { error = "user_not_found" });
        if (!user.IsActive) return Unauthorized(new { error = "account_inactive" });

        // Token rotation: eski token'ı revoke et, yeni token üret
        await _repo.RevokeRefreshToken(token);

        var access = _tokens.CreateAccessToken(user.Id, user.Email, user.Role);
        var newRefresh = _tokens.CreateRefreshToken();
        var expiresAt = _tokens.RefreshTokenExpiresAtUtc().DateTime;

        await _repo.InsertRefreshToken(user.Id, newRefresh, expiresAt);

        return Ok(new AuthResponse(access, newRefresh, _tokens.GetAccessTokenExpiresInSeconds()));
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        var userIdClaim = User.FindFirst("uid")?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized(new { error = "invalid_token" });

        // Kullanıcıya ait tüm refresh token'ları revoke et
        await _repo.RevokeAllUserTokens(userId);
        return Ok(new { ok = true });
    }

    [Authorize(Roles = "admin")]
    [HttpGet("admin-only")]
    public IActionResult AdminOnly()
    {
        return Ok(new { message = "Admin access granted" });
    }
}

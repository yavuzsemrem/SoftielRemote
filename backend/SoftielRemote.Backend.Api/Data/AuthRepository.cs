using BCrypt.Net;
using Dapper;
using System.Security.Cryptography;
using Npgsql;
using SoftielRemote.Backend.Api.Domain;

namespace SoftielRemote.Backend.Api.Data;

public sealed class AuthRepository
{
    private readonly IDbConnectionFactory _db;

    public AuthRepository(IDbConnectionFactory db) => _db = db;

    public async Task<User?> GetUserByEmail(string email)
    {
        const string sql = @"
SELECT 
    id AS Id,
    email AS Email,
    password_hash AS PasswordHash,
    role,
    is_active AS IsActive,
    created_at AS CreatedAt
FROM users
WHERE lower(email) = lower(@email)
LIMIT 1;";
        using var conn = _db.Create();
        return await conn.QuerySingleOrDefaultAsync<User>(sql, new { email });
    }

    public async Task<User?> GetUserById(Guid id)
    {
        const string sql = @"
SELECT 
    id AS Id,
    email AS Email,
    password_hash AS PasswordHash,
    role,
    is_active AS IsActive,
    created_at AS CreatedAt
FROM users
WHERE id = @id
LIMIT 1;";
        using var conn = _db.Create();
        return await conn.QuerySingleOrDefaultAsync<User>(sql, new { id });
    }

    public async Task<Guid> CreateUser(string email, string passwordPlain, string role = "user")
    {
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(passwordPlain, workFactor: 12);
        
        const string sql = @"
INSERT INTO users (email, password_hash, role, is_active)
VALUES (@email, @passwordHash, @role, true)
RETURNING id;";
        using var conn = _db.Create();
        try
        {
            return await conn.ExecuteScalarAsync<Guid>(sql, new { email, passwordHash, role });
        }
        catch (PostgresException ex) when (ex.SqlState == "23505") // unique_violation
        {
            throw new InvalidOperationException("Email already exists", ex);
        }
    }

    public Task<bool> VerifyPassword(User user, string passwordPlain)
    {
        return Task.FromResult(BCrypt.Net.BCrypt.Verify(passwordPlain, user.PasswordHash));
    }

    public async Task<Guid> InsertRefreshToken(Guid userId, string token, DateTime expiresAt)
    {
        var tokenHash = Sha256(token);
        const string sql = @"
INSERT INTO refresh_tokens (user_id, token, expires_at, is_revoked)
VALUES (@userId, @tokenHash, @expiresAt, false)
RETURNING id;";
        using var conn = _db.Create();
        return await conn.ExecuteScalarAsync<Guid>(sql, new { userId, tokenHash, expiresAt });
    }

    public async Task<RefreshToken?> GetRefreshToken(string token)
    {
        var tokenHash = Sha256(token);
        const string sql = @"
SELECT 
    id AS Id,
    user_id AS UserId,
    token AS Token,
    created_at AS CreatedAt,
    expires_at AS ExpiresAt,
    is_revoked AS IsRevoked
FROM refresh_tokens
WHERE token = @tokenHash
LIMIT 1;";
        using var conn = _db.Create();
        return await conn.QuerySingleOrDefaultAsync<RefreshToken>(sql, new { tokenHash });
    }

    public async Task RevokeRefreshToken(string token)
    {
        var tokenHash = Sha256(token);
        const string sql = @"
UPDATE refresh_tokens
SET is_revoked = true
WHERE token = @tokenHash;";
        using var conn = _db.Create();
        await conn.ExecuteAsync(sql, new { tokenHash });
    }

    public async Task RevokeAllUserTokens(Guid userId)
    {
        const string sql = @"
UPDATE refresh_tokens
SET is_revoked = true
WHERE user_id = @userId AND is_revoked = false;";
        using var conn = _db.Create();
        await conn.ExecuteAsync(sql, new { userId });
    }

    public static string Sha256(string value)
    {
        var bytes = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}

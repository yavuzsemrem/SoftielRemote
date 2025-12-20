using Dapper;
using SoftielRemote.Backend.Api.Domain.Entities;
using SoftielRemote.Backend.Api.Domain.Enums;

namespace SoftielRemote.Backend.Api.Data;

public sealed class SessionRepository
{
    private readonly IDbConnectionFactory _db;

    public SessionRepository(IDbConnectionFactory db) => _db = db;

    public async Task<Guid> CreateSession(Guid hostDeviceId, Guid? clientDeviceId = null)
    {
        var sessionId = Guid.NewGuid();
        const string sql = @"
INSERT INTO sessions (id, host_device_id, client_device_id, status, created_at)
VALUES (@id, @hostDeviceId, @clientDeviceId, @status, @createdAt)
RETURNING id;";
        using var conn = _db.Create();
        var result = await conn.ExecuteScalarAsync<Guid?>(sql, new
        {
            id = sessionId,
            hostDeviceId,
            clientDeviceId,
            status = SessionStatus.PendingApproval.ToString(),
            createdAt = DateTime.UtcNow
        });

        if (!result.HasValue || result.Value == Guid.Empty)
            throw new InvalidOperationException("Failed to create session: INSERT did not return a valid session ID");

        return result.Value;
    }

    public async Task<Session?> GetById(Guid sessionId)
    {
        const string sql = @"
SELECT 
    id AS Id,
    host_device_id AS HostDeviceId,
    client_device_id AS ClientDeviceId,
    status AS Status,
    created_at AS CreatedAt,
    approved_at AS ApprovedAt,
    connected_at AS ConnectedAt,
    ended_at AS EndedAt,
    end_reason AS EndReason
FROM sessions
WHERE id = @sessionId
LIMIT 1;";
        using var conn = _db.Create();
        return await conn.QuerySingleOrDefaultAsync<Session>(sql, new { sessionId });
    }

    public async Task<IEnumerable<Session>> GetPendingForHost(Guid hostDeviceId)
    {
        const string sql = @"
SELECT 
    id AS Id,
    host_device_id AS HostDeviceId,
    client_device_id AS ClientDeviceId,
    status AS Status,
    created_at AS CreatedAt,
    approved_at AS ApprovedAt,
    connected_at AS ConnectedAt,
    ended_at AS EndedAt,
    end_reason AS EndReason
FROM sessions
WHERE host_device_id = @hostDeviceId 
  AND status = @status
ORDER BY created_at DESC;";
        using var conn = _db.Create();
        var results = await conn.QueryAsync<dynamic>(sql, new
        {
            hostDeviceId,
            status = SessionStatus.PendingApproval.ToString()
        });

        return results.Select(r => new Session
        {
            Id = r.Id,
            HostDeviceId = r.HostDeviceId,
            ClientDeviceId = r.ClientDeviceId,
            Status = Enum.Parse<SessionStatus>(r.Status),
            CreatedAt = r.CreatedAt,
            ApprovedAt = r.ApprovedAt,
            ConnectedAt = r.ConnectedAt,
            EndedAt = r.EndedAt,
            EndReason = r.EndReason
        });
    }

    public async Task UpdateStatus(Guid sessionId, SessionStatus status, DateTime? approvedAt = null, DateTime? connectedAt = null)
    {
        var updates = new List<string> { "status = @status" };
        var parameters = new Dictionary<string, object?> { { "sessionId", sessionId }, { "status", status.ToString() } };

        if (approvedAt.HasValue)
        {
            updates.Add("approved_at = @approvedAt");
            parameters["approvedAt"] = approvedAt.Value;
        }

        if (connectedAt.HasValue)
        {
            updates.Add("connected_at = @connectedAt");
            parameters["connectedAt"] = connectedAt.Value;
        }

        var sql = $@"
UPDATE sessions
SET {string.Join(", ", updates)}
WHERE id = @sessionId;";
        using var conn = _db.Create();
        await conn.ExecuteAsync(sql, parameters);
    }

    public async Task EndSession(Guid sessionId, string? reason = null)
    {
        const string sql = @"
UPDATE sessions
SET status = @status, ended_at = @endedAt, end_reason = @reason
WHERE id = @sessionId;";
        using var conn = _db.Create();
        await conn.ExecuteAsync(sql, new
        {
            sessionId,
            status = SessionStatus.Ended.ToString(),
            endedAt = DateTime.UtcNow,
            reason
        });
    }
}


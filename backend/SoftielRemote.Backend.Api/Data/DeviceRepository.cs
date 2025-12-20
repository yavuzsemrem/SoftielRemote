using Dapper;
using SoftielRemote.Backend.Api.Domain.Entities;
using SoftielRemote.Backend.Api.Domain.Enums;

namespace SoftielRemote.Backend.Api.Data;

public sealed class DeviceRepository
{
    private readonly IDbConnectionFactory _db;

    public DeviceRepository(IDbConnectionFactory db) => _db = db;

    public async Task<Device?> GetByDeviceCode(string deviceCode)
    {
        const string sql = @"
SELECT 
    id AS Id,
    owner_user_id AS OwnerUserId,
    device_code AS DeviceCode,
    device_name AS DeviceName,
    device_type AS DeviceType,
    is_online AS IsOnline,
    last_seen_at AS LastSeenAt,
    created_at AS CreatedAt
FROM devices
WHERE device_code = @deviceCode
LIMIT 1;";
        using var conn = _db.Create();
        var device = await conn.QuerySingleOrDefaultAsync<Device>(sql, new { deviceCode });
        if (device == null) return null;

        // Domain invariant: Device must have an owner for session requests
        if (!device.OwnerUserId.HasValue || device.OwnerUserId.Value == Guid.Empty)
            throw new InvalidOperationException("Device has no owner assigned");

        return device;
    }

    public async Task<Device?> GetByDeviceCodeWithoutOwnerCheck(string deviceCode)
    {
        const string sql = @"
SELECT 
    id AS Id,
    owner_user_id AS OwnerUserId,
    device_code AS DeviceCode,
    device_name AS DeviceName,
    device_type AS DeviceType,
    is_online AS IsOnline,
    last_seen_at AS LastSeenAt,
    created_at AS CreatedAt
FROM devices
WHERE device_code = @deviceCode
LIMIT 1;";
        using var conn = _db.Create();
        return await conn.QuerySingleOrDefaultAsync<Device>(sql, new { deviceCode });
    }

    public async Task<Device?> GetById(Guid id)
    {
        const string sql = @"
SELECT 
    id AS Id,
    owner_user_id AS OwnerUserId,
    device_code AS DeviceCode,
    device_name AS DeviceName,
    device_type AS DeviceType,
    is_online AS IsOnline,
    last_seen_at AS LastSeenAt,
    created_at AS CreatedAt
FROM devices
WHERE id = @id
LIMIT 1;";
        using var conn = _db.Create();
        return await conn.QuerySingleOrDefaultAsync<Device>(sql, new { id });
    }

    public async Task<Guid> RegisterOrHeartbeat(Device device)
    {
        const string sql = @"
INSERT INTO devices (id, owner_user_id, device_code, device_name, device_type, is_online, last_seen_at, created_at)
VALUES (@id, @ownerUserId, @deviceCode, @deviceName, @deviceType, true, @lastSeenAt, @createdAt)
ON CONFLICT (device_code) 
DO UPDATE SET 
    is_online = true,
    last_seen_at = @lastSeenAt,
    device_name = @deviceName,
    device_type = @deviceType,
    owner_user_id = COALESCE(EXCLUDED.owner_user_id, devices.owner_user_id)
RETURNING id;";
        using var conn = _db.Create();
        try
        {
            return await conn.ExecuteScalarAsync<Guid>(sql, new
            {
                id = device.Id,
                ownerUserId = device.OwnerUserId,
                deviceCode = device.DeviceCode,
                deviceName = device.DeviceName,
                deviceType = device.DeviceType.ToString(),
                lastSeenAt = device.LastSeenAt,
                createdAt = device.CreatedAt
            });
        }
        catch (Npgsql.PostgresException ex) when (ex.SqlState == "23505") // unique_violation
        {
            // DeviceCode zaten var, update yapıldı, id'yi tekrar al
            var existing = await GetByDeviceCodeWithoutOwnerCheck(device.DeviceCode);
            return existing?.Id ?? device.Id;
        }
    }

    public async Task SetOnline(Guid deviceId)
    {
        const string sql = @"
UPDATE devices
SET is_online = true, last_seen_at = @now
WHERE id = @deviceId;";
        using var conn = _db.Create();
        await conn.ExecuteAsync(sql, new { deviceId, now = DateTime.UtcNow });
    }

    public async Task SetOffline(Guid deviceId)
    {
        const string sql = @"
UPDATE devices
SET is_online = false
WHERE id = @deviceId;";
        using var conn = _db.Create();
        await conn.ExecuteAsync(sql, new { deviceId });
    }

    public async Task Heartbeat(Guid deviceId)
    {
        const string sql = @"
UPDATE devices
SET is_online = true, last_seen_at = @now
WHERE id = @deviceId;";
        using var conn = _db.Create();
        await conn.ExecuteAsync(sql, new { deviceId, now = DateTime.UtcNow });
    }

    public async Task MarkOffline(Guid deviceId)
    {
        const string sql = @"
UPDATE devices
SET is_online = false
WHERE id = @deviceId;";
        using var conn = _db.Create();
        await conn.ExecuteAsync(sql, new { deviceId });
    }

    public async Task ClaimDevice(Guid deviceId, Guid ownerUserId)
    {
        const string sql = @"
UPDATE devices
SET owner_user_id = @ownerUserId
WHERE id = @deviceId AND owner_user_id IS NULL;";
        using var conn = _db.Create();
        var affectedRows = await conn.ExecuteAsync(sql, new { deviceId, ownerUserId });
        
        if (affectedRows == 0)
        {
            // Device yok ya da zaten claim edilmiş
            var device = await GetById(deviceId);
            if (device == null)
                throw new InvalidOperationException("Device not found");
            
            if (device.OwnerUserId.HasValue)
                throw new InvalidOperationException("Device is already claimed");
            
            throw new InvalidOperationException("Device could not be claimed");
        }
    }
}


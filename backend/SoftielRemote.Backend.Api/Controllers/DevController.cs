using System.Security.Claims;
using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using SoftielRemote.Backend.Api.Controllers.Requests;
using SoftielRemote.Backend.Api.Data;
using SoftielRemote.Backend.Api.Domain.Entities;
using SoftielRemote.Backend.Api.Domain.Enums;
using SoftielRemote.Backend.Api.Infrastructure;
using SoftielRemote.Backend.Api.Services;

namespace SoftielRemote.Backend.Api.Controllers;

[ApiController]
[Route("dev")]
[Authorize]
public sealed class DevController : ControllerBase
{
    private readonly IDbConnectionFactory _db;
    private readonly IWebHostEnvironment _env;
    private readonly SessionNotificationService _notificationService;
    private readonly DeviceRepository _deviceRepo;
    private readonly DeviceNotificationService _deviceNotificationService;

    public DevController(
        IDbConnectionFactory db, 
        IWebHostEnvironment env, 
        SessionNotificationService notificationService,
        DeviceRepository deviceRepo,
        DeviceNotificationService deviceNotificationService)
    {
        _db = db;
        _env = env;
        _notificationService = notificationService;
        _deviceRepo = deviceRepo;
        _deviceNotificationService = deviceNotificationService;
    }

    [HttpGet("sessions")]
    public async Task<IActionResult> GetSessions()
    {
        if (!_env.IsDevelopment())
            return NotFound();
        
        // Tüm session'ları döndür (dev only)
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
ORDER BY created_at DESC
LIMIT 100;";
        
        using var conn = _db.Create();
        var results = await conn.QueryAsync<dynamic>(sql);
        
        return Ok(results);
    }

    [HttpGet("devices")]
    public async Task<IActionResult> GetDevices()
    {
        if (!_env.IsDevelopment())
            return NotFound();
        
        // Tüm device'ları döndür (dev only)
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
ORDER BY created_at DESC
LIMIT 100;";
        
        using var conn = _db.Create();
        var results = await conn.QueryAsync<dynamic>(sql);
        
        return Ok(results);
    }

    [HttpPost("emit-test")]
    public async Task<IActionResult> EmitTestEvent([FromBody] EmitTestRequest req)
    {
        if (!_env.IsDevelopment())
            return NotFound();

        var userIdClaim = User.FindFirst("uid")?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized(new { error = "invalid_token" });

        // Sahte session event'i gönder
        var fakeSession = new Domain.Entities.Session
        {
            Id = req.SessionId ?? Guid.NewGuid(),
            HostDeviceId = req.HostDeviceId ?? Guid.NewGuid(),
            ClientDeviceId = req.ClientDeviceId ?? Guid.NewGuid(),
            Status = Domain.Enums.SessionStatus.PendingApproval,
            CreatedAt = DateTime.UtcNow
        };

        var eventType = req.EventType?.ToLowerInvariant() ?? "SessionRequested";
        switch (eventType)
        {
            case "sessionrequested":
                await _notificationService.NotifySessionRequested(fakeSession);
                break;
            case "sessionapproved":
                var approvedSession = new Domain.Entities.Session
                {
                    Id = fakeSession.Id,
                    HostDeviceId = fakeSession.HostDeviceId,
                    ClientDeviceId = fakeSession.ClientDeviceId,
                    Status = Domain.Enums.SessionStatus.Approved,
                    CreatedAt = fakeSession.CreatedAt
                };
                await _notificationService.NotifySessionApproved(approvedSession);
                break;
            case "sessionrejected":
                var rejectedSession = new Domain.Entities.Session
                {
                    Id = fakeSession.Id,
                    HostDeviceId = fakeSession.HostDeviceId,
                    ClientDeviceId = fakeSession.ClientDeviceId,
                    Status = Domain.Enums.SessionStatus.Rejected,
                    CreatedAt = fakeSession.CreatedAt
                };
                await _notificationService.NotifySessionRejected(rejectedSession, req.Reason);
                break;
            case "sessionconnected":
                var connectedSession = new Domain.Entities.Session
                {
                    Id = fakeSession.Id,
                    HostDeviceId = fakeSession.HostDeviceId,
                    ClientDeviceId = fakeSession.ClientDeviceId,
                    Status = Domain.Enums.SessionStatus.Connected,
                    CreatedAt = fakeSession.CreatedAt
                };
                await _notificationService.NotifySessionConnected(connectedSession);
                break;
            case "sessionended":
                var endedSession = new Domain.Entities.Session
                {
                    Id = fakeSession.Id,
                    HostDeviceId = fakeSession.HostDeviceId,
                    ClientDeviceId = fakeSession.ClientDeviceId,
                    Status = Domain.Enums.SessionStatus.Ended,
                    CreatedAt = fakeSession.CreatedAt
                };
                await _notificationService.NotifySessionEnded(endedSession, req.Reason);
                break;
            default:
                return BadRequest(new { error = "invalid_event_type", validTypes = new[] { "SessionRequested", "SessionApproved", "SessionRejected", "SessionConnected", "SessionEnded" } });
        }

        return Ok(new { message = $"Event '{eventType}' sent", sessionId = fakeSession.Id });
    }

    [HttpPost("devices/{id:guid}/online")]
    public async Task<IActionResult> SetDeviceOnline(Guid id)
    {
        if (!_env.IsDevelopment())
            return NotFound();

        var userIdClaim = User.FindFirst("uid")?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized(new { error = "invalid_token" });

        var device = await _deviceRepo.GetById(id);
        if (device == null)
            return NotFound(new { error = "device_not_found" });

        // Authorization: Sadece device owner işlem yapabilir
        if (device.OwnerUserId.HasValue && device.OwnerUserId.Value != userId)
            return Forbid();

        await _deviceRepo.Heartbeat(id);

        // DeviceOnline event'ini gönder
        var updatedDevice = await _deviceRepo.GetById(id);
        if (updatedDevice != null)
        {
            await _deviceNotificationService.NotifyDeviceOnline(updatedDevice);
        }

        return Ok(new { message = "Device set to online", deviceId = id });
    }

    [HttpPost("devices/{id:guid}/offline")]
    public async Task<IActionResult> SetDeviceOffline(Guid id)
    {
        if (!_env.IsDevelopment())
            return NotFound();

        var userIdClaim = User.FindFirst("uid")?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized(new { error = "invalid_token" });

        var device = await _deviceRepo.GetById(id);
        if (device == null)
            return NotFound(new { error = "device_not_found" });

        // Authorization: Sadece device owner işlem yapabilir
        if (device.OwnerUserId.HasValue && device.OwnerUserId.Value != userId)
            return Forbid();

        await _deviceRepo.MarkOffline(id);

        // DeviceOffline event'ini gönder
        var updatedDevice = await _deviceRepo.GetById(id);
        if (updatedDevice != null)
        {
            await _deviceNotificationService.NotifyDeviceOffline(updatedDevice);
        }

        return Ok(new { message = "Device set to offline", deviceId = id });
    }

    [HttpPost("devices/register")]
    public async Task<IActionResult> RegisterDevice([FromBody] DeviceRegisterRequest req)
    {
        if (!_env.IsDevelopment())
            return NotFound();

        if (string.IsNullOrWhiteSpace(req.DeviceName))
            return BadRequest(new { error = "device_name_required" });

        if (!Enum.TryParse<DeviceType>(req.DeviceType, ignoreCase: true, out var deviceType))
            return BadRequest(new { error = "invalid_device_type", validTypes = new[] { "Desktop", "Laptop", "Mobile", "Server", "Windows", "Mac", "Linux" } });

        // DeviceCode üret (unique olana kadar dene)
        string deviceCode;
        int attempts = 0;
        do
        {
            deviceCode = DeviceCodeGenerator.Generate();
            var existing = await _deviceRepo.GetByDeviceCodeWithoutOwnerCheck(deviceCode);
            if (existing == null) break;
            attempts++;
            if (attempts > 10) // Güvenlik için limit
                return StatusCode(500, new { error = "device_code_generation_failed" });
        } while (true);

        // Device oluştur ve kaydet
        var device = new Device
        {
            Id = Guid.NewGuid(),
            OwnerUserId = null, // Dev endpoint'te owner yok
            DeviceCode = deviceCode,
            DeviceName = req.DeviceName,
            DeviceType = deviceType,
            IsOnline = true,
            LastSeenAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        var deviceId = await _deviceRepo.RegisterOrHeartbeat(device);

        // Kaydedilen device'ı al
        var registeredDevice = await _deviceRepo.GetById(deviceId);
        if (registeredDevice == null)
            return NotFound();

        return Ok(new DeviceRegisterResponse
        {
            Id = registeredDevice.Id,
            DeviceCode = registeredDevice.DeviceCode,
            DeviceName = registeredDevice.DeviceName,
            DeviceType = registeredDevice.DeviceType.ToString()
        });
    }

    /// <summary>
    /// Device Claim (Owner Binding) - Bir device'ı kullanıcıya bağlar.
    /// Bu işlem sadece 1 kez yapılabilir (owner_user_id NULL ise set edilir).
    /// 
    /// SWAGGER TEST AKIŞI:
    /// 1. Login yap:
    ///    POST /auth/login
    ///    { "email": "test@example.com", "password": "password123" }
    ///    → accessToken al
    /// 
    /// 2. Device register (Development only):
    ///    POST /dev/devices/register
    ///    { "deviceName": "Test Device", "deviceType": "Desktop" }
    ///    → deviceCode al (örn: "123 456 789")
    /// 
    /// 3. Device claim:
    ///    POST /devices/claim
    ///    Authorization: Bearer {accessToken}
    ///    { "deviceCode": "123 456 789" }
    ///    → 200 OK: { "deviceId": "...", "deviceCode": "...", "ownerUserId": "..." }
    /// 
    /// 4. Session request (artık 200 dönmeli):
    ///    POST /sessions/request
    ///    Authorization: Bearer {accessToken}
    ///    { "deviceCode": "123 456 789" }
    ///    → 200 OK: { "sessionId": "...", "status": "PendingApproval" }
    /// 
    /// NOT: Claim işlemi sadece 1 kez yapılabilir. İkinci claim denemesi 409 Conflict döner.
    /// </summary>
    [HttpPost("/devices/claim")]
    public async Task<IActionResult> ClaimDevice([FromBody] DeviceClaimRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.DeviceCode))
            return BadRequest(new { error = "device_code_required" });

        // JWT'den current user id al
        var userIdClaim = User.FindFirst("uid")?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var ownerUserId))
            return Unauthorized(new { error = "invalid_token" });

        // Device'ı bul (owner kontrolü olmadan)
        var device = await _deviceRepo.GetByDeviceCodeWithoutOwnerCheck(req.DeviceCode);
        if (device == null)
            return NotFound(new { error = "device_not_found" });

        // owner_user_id doluysa → 409 Conflict
        if (device.OwnerUserId.HasValue)
            return Conflict(new { error = "device_already_claimed", message = "Device is already claimed" });

        // ClaimDevice çağır
        try
        {
            await _deviceRepo.ClaimDevice(device.Id, ownerUserId);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = "device_claim_failed", message = ex.Message });
        }

        // Güncellenmiş device'ı al
        var claimedDevice = await _deviceRepo.GetById(device.Id);
        if (claimedDevice == null)
            return NotFound(new { error = "device_not_found" });

        // 200 OK dön
        return Ok(new
        {
            deviceId = claimedDevice.Id,
            deviceCode = claimedDevice.DeviceCode,
            ownerUserId = claimedDevice.OwnerUserId
        });
    }
}

public sealed class EmitTestRequest
{
    public string? EventType { get; set; }
    public Guid? SessionId { get; set; }
    public Guid? HostDeviceId { get; set; }
    public Guid? ClientDeviceId { get; set; }
    public string? Reason { get; set; }
}

public sealed class DeviceRegisterRequest
{
    public string DeviceName { get; set; } = default!;
    public string DeviceType { get; set; } = default!;
}

public sealed class DeviceRegisterResponse
{
    public Guid Id { get; set; }
    public string DeviceCode { get; set; } = default!;
    public string DeviceName { get; set; } = default!;
    public string DeviceType { get; set; } = default!;
}


using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SoftielRemote.Backend.Api.Contracts;
using SoftielRemote.Backend.Api.Data;
using SoftielRemote.Backend.Api.Domain.Entities;
using SoftielRemote.Backend.Api.Domain.Enums;
using SoftielRemote.Backend.Api.Services;

namespace SoftielRemote.Backend.Api.Controllers;

[ApiController]
[Route("sessions")]
[Authorize]
public sealed class SessionController : ControllerBase
{
    private readonly SessionRepository _sessionRepo;
    private readonly DeviceRepository _deviceRepo;
    private readonly SessionNotificationService _notificationService;

    public SessionController(SessionRepository sessionRepo, DeviceRepository deviceRepo, SessionNotificationService notificationService)
    {
        _sessionRepo = sessionRepo;
        _deviceRepo = deviceRepo;
        _notificationService = notificationService;
    }

    [HttpPost("request")]
    public async Task<IActionResult> RequestSession([FromBody] SessionRequestRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.DeviceCode))
            return BadRequest(new { error = "device_code_required" });

        // Host device'ı bul
        Device? hostDevice;
        try
        {
            hostDevice = await _deviceRepo.GetByDeviceCode(req.DeviceCode);
        }
        catch (InvalidOperationException ex) when (ex.Message == "Device has no owner assigned")
        {
            return Conflict(new { error = "device_not_properly_registered", message = "Device is not properly registered" });
        }

        if (hostDevice == null)
            return NotFound(new { error = "device_not_found", message = "Device not found" });

        // Host device owner_user_id NULL kontrolü
        if (!hostDevice.OwnerUserId.HasValue)
            return Conflict(new { error = "device_not_properly_registered", message = "Host device is not properly registered" });

        // Session Guard: Host offline ise 409 Conflict
        if (!hostDevice.IsOnline)
            return Conflict(new { error = "host_device_offline", message = "Host device is offline" });

        // Client device ID'yi JWT'den al (opsiyonel, login olmadan da olabilir)
        Guid? clientDeviceId = null;
        Device? clientDevice = null;
        var userIdClaim = User.FindFirst("uid")?.Value;
        if (!string.IsNullOrEmpty(userIdClaim) && Guid.TryParse(userIdClaim, out var userId))
        {
            // TODO: User'a ait device'ı bul (şimdilik null bırakıyoruz)
            // Client device varsa owner kontrolü yap
            if (clientDeviceId.HasValue)
            {
                clientDevice = await _deviceRepo.GetById(clientDeviceId.Value);
                if (clientDevice != null && !clientDevice.OwnerUserId.HasValue)
                {
                    return Conflict(new { error = "device_not_properly_registered", message = "Client device is not properly registered" });
                }
            }
        }

        // Session oluştur (PendingApproval durumunda)
        var sessionId = await _sessionRepo.CreateSession(hostDevice.Id, clientDeviceId);

        // sessionId Guid.Empty kontrolü
        if (sessionId == Guid.Empty)
            throw new InvalidOperationException("Session creation failed: sessionId is empty");

        // SessionRequested event'ini gönder (session entity oluşturarak)
        var session = new Session
        {
            Id = sessionId,
            HostDeviceId = hostDevice.Id,
            ClientDeviceId = clientDeviceId ?? Guid.Empty,
            Status = SessionStatus.PendingApproval,
            CreatedAt = DateTime.UtcNow
        };
        await _notificationService.NotifySessionRequested(session);

        return Ok(new { sessionId, status = SessionStatus.PendingApproval.ToString() });
    }

    [HttpPost("{sessionId:guid}/approve")]
    public async Task<IActionResult> ApproveSession(Guid sessionId)
    {
        var session = await _sessionRepo.GetById(sessionId);
        if (session == null)
            return NotFound(new { error = "session_not_found" });

        // Host device ownership kontrolü
        var userIdClaim = User.FindFirst("uid")?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized(new { error = "invalid_token" });

        var hostDevice = await _deviceRepo.GetById(session.HostDeviceId);
        if (hostDevice == null)
            return NotFound(new { error = "host_device_not_found" });

        // Host device owner kontrolü (owner null ise herkes onaylayabilir)
        if (hostDevice.OwnerUserId.HasValue && hostDevice.OwnerUserId.Value != userId)
            return Forbid();

        // Status kontrolü
        if (session.Status != SessionStatus.PendingApproval)
            return BadRequest(new { error = "invalid_session_status", currentStatus = session.Status.ToString() });

        // Approve
        await _sessionRepo.UpdateStatus(sessionId, SessionStatus.Approved, approvedAt: DateTime.UtcNow);

        // SessionApproved event'ini gönder
        var updatedSession = await _sessionRepo.GetById(sessionId);
        if (updatedSession != null)
        {
            await _notificationService.NotifySessionApproved(updatedSession);
        }

        return Ok(new { sessionId, status = SessionStatus.Approved.ToString() });
    }

    [HttpPost("{sessionId:guid}/reject")]
    public async Task<IActionResult> RejectSession(Guid sessionId)
    {
        var session = await _sessionRepo.GetById(sessionId);
        if (session == null)
            return NotFound(new { error = "session_not_found" });

        // Host device ownership kontrolü
        var userIdClaim = User.FindFirst("uid")?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized(new { error = "invalid_token" });

        var hostDevice = await _deviceRepo.GetById(session.HostDeviceId);
        if (hostDevice == null)
            return NotFound(new { error = "host_device_not_found" });

        // Host device owner kontrolü
        if (hostDevice.OwnerUserId.HasValue && hostDevice.OwnerUserId.Value != userId)
            return Forbid();

        // Status kontrolü
        if (session.Status != SessionStatus.PendingApproval)
            return BadRequest(new { error = "invalid_session_status", currentStatus = session.Status.ToString() });

        // Reject → Ended
        await _sessionRepo.UpdateStatus(sessionId, SessionStatus.Rejected);
        await _sessionRepo.EndSession(sessionId, "rejected_by_host");

        // SessionRejected event'ini gönder
        var updatedSession = await _sessionRepo.GetById(sessionId);
        if (updatedSession != null)
        {
            await _notificationService.NotifySessionRejected(updatedSession, "rejected_by_host");
        }

        return Ok(new { sessionId, status = SessionStatus.Rejected.ToString() });
    }

    [HttpPost("{sessionId:guid}/connect")]
    public async Task<IActionResult> ConnectSession(Guid sessionId)
    {
        var session = await _sessionRepo.GetById(sessionId);
        if (session == null)
            return NotFound(new { error = "session_not_found" });

        // Status kontrolü: Approved olmalı
        if (session.Status != SessionStatus.Approved)
            return BadRequest(new { error = "session_not_approved", currentStatus = session.Status.ToString() });

        // Connect
        await _sessionRepo.UpdateStatus(sessionId, SessionStatus.Connected, connectedAt: DateTime.UtcNow);

        // SessionConnected event'ini gönder
        var updatedSession = await _sessionRepo.GetById(sessionId);
        if (updatedSession != null)
        {
            await _notificationService.NotifySessionConnected(updatedSession);
        }

        return Ok(new { sessionId, status = SessionStatus.Connected.ToString() });
    }

    [HttpPost("{sessionId:guid}/end")]
    public async Task<IActionResult> EndSession(Guid sessionId, [FromBody] string? reason = null)
    {
        var session = await _sessionRepo.GetById(sessionId);
        if (session == null)
            return NotFound(new { error = "session_not_found" });

        // Host veya client end edebilir
        var userIdClaim = User.FindFirst("uid")?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized(new { error = "invalid_token" });

        var hostDevice = await _deviceRepo.GetById(session.HostDeviceId);
        if (hostDevice == null)
            return NotFound(new { error = "host_device_not_found" });

        // Host device owner kontrolü (owner null ise herkes end edebilir)
        var isHost = hostDevice.OwnerUserId.HasValue && hostDevice.OwnerUserId.Value == userId;
        var isClient = session.ClientDeviceId != Guid.Empty; // TODO: Client device ownership kontrolü

        if (!isHost && !isClient)
            return Forbid();

        // End
        await _sessionRepo.EndSession(sessionId, reason);

        // SessionEnded event'ini gönder
        var updatedSession = await _sessionRepo.GetById(sessionId);
        if (updatedSession != null)
        {
            await _notificationService.NotifySessionEnded(updatedSession, reason);
        }

        return Ok(new { sessionId, status = SessionStatus.Ended.ToString() });
    }
}


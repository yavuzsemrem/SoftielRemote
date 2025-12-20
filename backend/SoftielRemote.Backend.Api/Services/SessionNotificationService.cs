using Microsoft.AspNetCore.SignalR;
using SoftielRemote.Backend.Api.Data;
using SoftielRemote.Backend.Api.Domain.Entities;
using SoftielRemote.Backend.Api.Hubs;

namespace SoftielRemote.Backend.Api.Services;

public sealed class SessionNotificationService
{
    private readonly IHubContext<SessionHub> _hubContext;
    private readonly DeviceRepository _deviceRepo;

    public SessionNotificationService(IHubContext<SessionHub> hubContext, DeviceRepository deviceRepo)
    {
        _hubContext = hubContext;
        _deviceRepo = deviceRepo;
    }

    public async Task NotifySessionRequested(Session session)
    {
        var hostDevice = await _deviceRepo.GetById(session.HostDeviceId);
        if (hostDevice?.OwnerUserId == null) return;

        var payload = new
        {
            sessionId = session.Id,
            status = session.Status.ToString(),
            hostDeviceId = session.HostDeviceId,
            clientDeviceId = session.ClientDeviceId,
            timestamp = DateTime.UtcNow
        };

        await _hubContext.Clients.Group($"user:{hostDevice.OwnerUserId.Value}").SendAsync("SessionRequested", payload);
    }

    public async Task NotifySessionApproved(Session session)
    {
        var clientDevice = await _deviceRepo.GetById(session.ClientDeviceId);
        if (clientDevice?.OwnerUserId == null) return;

        var payload = new
        {
            sessionId = session.Id,
            status = session.Status.ToString(),
            hostDeviceId = session.HostDeviceId,
            clientDeviceId = session.ClientDeviceId,
            timestamp = DateTime.UtcNow
        };

        await _hubContext.Clients.Group($"user:{clientDevice.OwnerUserId.Value}").SendAsync("SessionApproved", payload);
    }

    public async Task NotifySessionRejected(Session session, string? reason = null)
    {
        var clientDevice = await _deviceRepo.GetById(session.ClientDeviceId);
        if (clientDevice?.OwnerUserId == null) return;

        var payload = new
        {
            sessionId = session.Id,
            status = session.Status.ToString(),
            hostDeviceId = session.HostDeviceId,
            clientDeviceId = session.ClientDeviceId,
            reason,
            timestamp = DateTime.UtcNow
        };

        await _hubContext.Clients.Group($"user:{clientDevice.OwnerUserId.Value}").SendAsync("SessionRejected", payload);
    }

    public async Task NotifySessionConnected(Session session)
    {
        var hostDevice = await _deviceRepo.GetById(session.HostDeviceId);
        var hostUserId = hostDevice?.OwnerUserId;

        Guid? clientUserId = null;
        if (session.ClientDeviceId != Guid.Empty)
        {
            var clientDevice = await _deviceRepo.GetById(session.ClientDeviceId);
            clientUserId = clientDevice?.OwnerUserId;
        }

        var payload = new
        {
            sessionId = session.Id,
            status = session.Status.ToString(),
            hostDeviceId = session.HostDeviceId,
            clientDeviceId = session.ClientDeviceId,
            timestamp = DateTime.UtcNow
        };

        var tasks = new List<Task>();

        if (hostUserId.HasValue)
        {
            tasks.Add(_hubContext.Clients.Group($"user:{hostUserId}").SendAsync("SessionConnected", payload));
        }

        if (clientUserId.HasValue)
        {
            tasks.Add(_hubContext.Clients.Group($"user:{clientUserId}").SendAsync("SessionConnected", payload));
        }

        await Task.WhenAll(tasks);
    }

    public async Task NotifySessionEnded(Session session, string? reason = null)
    {
        var hostDevice = await _deviceRepo.GetById(session.HostDeviceId);
        var hostUserId = hostDevice?.OwnerUserId;

        Guid? clientUserId = null;
        if (session.ClientDeviceId != Guid.Empty)
        {
            var clientDevice = await _deviceRepo.GetById(session.ClientDeviceId);
            clientUserId = clientDevice?.OwnerUserId;
        }

        var payload = new
        {
            sessionId = session.Id,
            status = session.Status.ToString(),
            hostDeviceId = session.HostDeviceId,
            clientDeviceId = session.ClientDeviceId,
            reason,
            timestamp = DateTime.UtcNow
        };

        var tasks = new List<Task>();

        if (hostUserId.HasValue)
        {
            tasks.Add(_hubContext.Clients.Group($"user:{hostUserId}").SendAsync("SessionEnded", payload));
        }

        if (clientUserId.HasValue)
        {
            tasks.Add(_hubContext.Clients.Group($"user:{clientUserId}").SendAsync("SessionEnded", payload));
        }

        await Task.WhenAll(tasks);
    }
}


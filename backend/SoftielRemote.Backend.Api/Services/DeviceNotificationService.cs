using Microsoft.AspNetCore.SignalR;
using SoftielRemote.Backend.Api.Domain.Entities;
using SoftielRemote.Backend.Api.Hubs;

namespace SoftielRemote.Backend.Api.Services;

public sealed class DeviceNotificationService
{
    private readonly IHubContext<SessionHub> _hubContext;

    public DeviceNotificationService(IHubContext<SessionHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task NotifyDeviceOnline(Device device)
    {
        if (device.OwnerUserId == null) return;

        var payload = new
        {
            deviceId = device.Id,
            deviceCode = device.DeviceCode,
            deviceName = device.DeviceName,
            isOnline = true,
            lastSeenAt = device.LastSeenAt,
            timestamp = DateTime.UtcNow
        };

        await _hubContext.Clients.Group($"user:{device.OwnerUserId}").SendAsync("DeviceOnline", payload);
    }

    public async Task NotifyDeviceOffline(Device device)
    {
        if (device.OwnerUserId == null) return;

        var payload = new
        {
            deviceId = device.Id,
            deviceCode = device.DeviceCode,
            deviceName = device.DeviceName,
            isOnline = false,
            timestamp = DateTime.UtcNow
        };

        await _hubContext.Clients.Group($"user:{device.OwnerUserId}").SendAsync("DeviceOffline", payload);
    }
}


using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using SoftielRemote.Backend.Api.Data;
using SoftielRemote.Backend.Api.Services;

namespace SoftielRemote.Backend.Api.Hubs;

[Authorize]
public sealed class SessionHub : Hub
{
    private readonly DeviceRepository _deviceRepo;
    private readonly DeviceNotificationService _deviceNotificationService;

    public SessionHub(DeviceRepository deviceRepo, DeviceNotificationService deviceNotificationService)
    {
        _deviceRepo = deviceRepo;
        _deviceNotificationService = deviceNotificationService;
    }

    public override async Task OnConnectedAsync()
    {
        var userIdClaim = Context.User?.FindFirst("uid")?.Value;
        if (string.IsNullOrEmpty(userIdClaim))
        {
            Context.Abort();
            return;
        }

        var groupName = $"user:{userIdClaim}";
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);

        // Device presence: DeviceId query parameter veya claim'den al
        var deviceIdParam = Context.GetHttpContext()?.Request.Query["deviceId"].ToString();
        if (!string.IsNullOrEmpty(deviceIdParam) && Guid.TryParse(deviceIdParam, out var deviceId))
        {
            var device = await _deviceRepo.GetById(deviceId);
            if (device != null)
            {
                // Authorization: Sadece device owner heartbeat yapabilir
                if (device.OwnerUserId.HasValue && device.OwnerUserId.Value.ToString() == userIdClaim)
                {
                    await _deviceRepo.Heartbeat(deviceId);
                    
                    // DeviceOnline event'ini gönder
                    var updatedDevice = await _deviceRepo.GetById(deviceId);
                    if (updatedDevice != null)
                    {
                        await _deviceNotificationService.NotifyDeviceOnline(updatedDevice);
                    }
                }
            }
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userIdClaim = Context.User?.FindFirst("uid")?.Value;
        if (!string.IsNullOrEmpty(userIdClaim))
        {
            var groupName = $"user:{userIdClaim}";
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        }

        // Device presence: DeviceId query parameter veya claim'den al
        var deviceIdParam = Context.GetHttpContext()?.Request.Query["deviceId"].ToString();
        if (!string.IsNullOrEmpty(deviceIdParam) && Guid.TryParse(deviceIdParam, out var deviceId))
        {
            var device = await _deviceRepo.GetById(deviceId);
            if (device != null)
            {
                // Authorization: Sadece device owner offline yapabilir
                if (device.OwnerUserId.HasValue && device.OwnerUserId.Value.ToString() == userIdClaim)
                {
                    await _deviceRepo.MarkOffline(deviceId);
                    
                    // DeviceOffline event'ini gönder
                    var updatedDevice = await _deviceRepo.GetById(deviceId);
                    if (updatedDevice != null)
                    {
                        await _deviceNotificationService.NotifyDeviceOffline(updatedDevice);
                    }
                }
            }
        }

        await base.OnDisconnectedAsync(exception);
    }
}


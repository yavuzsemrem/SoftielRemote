using SoftielRemote.Core.Enums;

namespace SoftielRemote.Core.Dtos;

/// <summary>
/// Bekleyen bağlantı isteği.
/// </summary>
public class PendingConnectionRequest
{
    public string ConnectionId { get; set; } = string.Empty;
    public string TargetDeviceId { get; set; } = string.Empty;
    public string? RequesterId { get; set; }
    public string? RequesterName { get; set; }
    public string? RequesterIp { get; set; }
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    public ConnectionStatus Status { get; set; } = ConnectionStatus.Pending;
}


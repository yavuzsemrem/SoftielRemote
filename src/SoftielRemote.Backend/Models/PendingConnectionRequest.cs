using SoftielRemote.Core.Dtos;

namespace SoftielRemote.Backend.Models;

/// <summary>
/// Bekleyen bağlantı isteği (Backend'de kullanılan model).
/// Core.Dtos.PendingConnectionRequest ile aynı, ama Backend'de repository için kullanılıyor.
/// </summary>
public class PendingConnectionRequest
{
    public string ConnectionId { get; set; } = string.Empty;
    public string TargetDeviceId { get; set; } = string.Empty;
    public string? RequesterId { get; set; }
    public string? RequesterName { get; set; }
    public string? RequesterIp { get; set; }
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    public Core.Enums.ConnectionStatus Status { get; set; } = Core.Enums.ConnectionStatus.Pending;
}


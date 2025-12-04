using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SoftielRemote.Core.Enums;

namespace SoftielRemote.Backend.Models;

/// <summary>
/// PostgreSQL'de saklanan Connection Request entity.
/// </summary>
[Table("ConnectionRequests")]
public class ConnectionRequestEntity
{
    /// <summary>
    /// Bağlantı isteğinin benzersiz ID'si (Primary Key).
    /// </summary>
    [Key]
    [MaxLength(100)]
    public string ConnectionId { get; set; } = string.Empty;

    /// <summary>
    /// Hedef Agent'ın Device ID'si.
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string TargetDeviceId { get; set; } = string.Empty;

    /// <summary>
    /// İstek yapan kişinin ID'si (opsiyonel).
    /// </summary>
    [MaxLength(50)]
    public string? RequesterId { get; set; }

    /// <summary>
    /// İstek yapan kişinin adı (opsiyonel).
    /// </summary>
    [MaxLength(255)]
    public string? RequesterName { get; set; }

    /// <summary>
    /// İstek yapan kişinin IP adresi (opsiyonel).
    /// </summary>
    [MaxLength(45)]
    public string? RequesterIp { get; set; }

    /// <summary>
    /// İstek zamanı.
    /// </summary>
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Bağlantı durumu.
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = ConnectionStatus.Pending.ToString();

    /// <summary>
    /// Core.Enums.ConnectionStatus'a dönüştürür.
    /// </summary>
    public ConnectionStatus GetStatus()
    {
        return Enum.TryParse<ConnectionStatus>(Status, out var status) 
            ? status 
            : ConnectionStatus.Pending;
    }

    /// <summary>
    /// Core.Dtos.PendingConnectionRequest'e dönüştürür.
    /// </summary>
    public Core.Dtos.PendingConnectionRequest ToCorePendingConnectionRequest()
    {
        return new Core.Dtos.PendingConnectionRequest
        {
            ConnectionId = ConnectionId,
            TargetDeviceId = TargetDeviceId,
            RequesterId = RequesterId,
            RequesterName = RequesterName,
            RequesterIp = RequesterIp,
            RequestedAt = RequestedAt,
            Status = GetStatus()
        };
    }

    /// <summary>
    /// Backend.Models.PendingConnectionRequest'e dönüştürür.
    /// </summary>
    public Backend.Models.PendingConnectionRequest ToPendingConnectionRequest()
    {
        return new Backend.Models.PendingConnectionRequest
        {
            ConnectionId = ConnectionId,
            TargetDeviceId = TargetDeviceId,
            RequesterId = RequesterId,
            RequesterName = RequesterName,
            RequesterIp = RequesterIp,
            RequestedAt = RequestedAt,
            Status = GetStatus()
        };
    }

    /// <summary>
    /// Core.Dtos.PendingConnectionRequest'ten oluşturur.
    /// </summary>
    public static ConnectionRequestEntity FromPendingConnectionRequest(Core.Dtos.PendingConnectionRequest request)
    {
        return new ConnectionRequestEntity
        {
            ConnectionId = request.ConnectionId,
            TargetDeviceId = request.TargetDeviceId,
            RequesterId = request.RequesterId,
            RequesterName = request.RequesterName,
            RequesterIp = request.RequesterIp,
            RequestedAt = request.RequestedAt,
            Status = request.Status.ToString()
        };
    }
}


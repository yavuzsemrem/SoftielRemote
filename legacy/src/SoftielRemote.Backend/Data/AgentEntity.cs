using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SoftielRemote.Backend.Models;

/// <summary>
/// PostgreSQL'de saklanan Agent entity.
/// </summary>
[Table("Agents")]
public class AgentEntity
{
    /// <summary>
    /// Agent'ın benzersiz Device ID'si (Primary Key).
    /// </summary>
    [Key]
    [MaxLength(50)]
    public string DeviceId { get; set; } = string.Empty;

    /// <summary>
    /// Agent'ın makine adı.
    /// </summary>
    [Required]
    [MaxLength(255)]
    public string MachineName { get; set; } = string.Empty;

    /// <summary>
    /// Agent'ın işletim sistemi bilgisi.
    /// </summary>
    [MaxLength(100)]
    public string OperatingSystem { get; set; } = string.Empty;

    /// <summary>
    /// Agent'ın son görülme zamanı (heartbeat için).
    /// </summary>
    public DateTime LastSeen { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Agent'ın SignalR connection ID'si (varsa).
    /// </summary>
    [MaxLength(100)]
    public string? ConnectionId { get; set; }

    /// <summary>
    /// Agent'ın IP adresi (TCP bağlantısı için).
    /// </summary>
    [MaxLength(45)]
    public string? IpAddress { get; set; }

    /// <summary>
    /// Agent'ın TCP port numarası.
    /// </summary>
    public int TcpPort { get; set; } = 8888;

    /// <summary>
    /// AgentInfo'ya dönüştürür.
    /// </summary>
    public AgentInfo ToAgentInfo()
    {
        return new AgentInfo
        {
            DeviceId = DeviceId,
            MachineName = MachineName,
            OperatingSystem = OperatingSystem,
            LastSeen = LastSeen,
            ConnectionId = ConnectionId,
            IpAddress = IpAddress,
            TcpPort = TcpPort // int to int? conversion is automatic
        };
    }

    /// <summary>
    /// AgentInfo'dan oluşturur.
    /// </summary>
    public static AgentEntity FromAgentInfo(AgentInfo agentInfo)
    {
        return new AgentEntity
        {
            DeviceId = agentInfo.DeviceId,
            MachineName = agentInfo.MachineName,
            OperatingSystem = agentInfo.OperatingSystem,
            LastSeen = agentInfo.LastSeen,
            ConnectionId = agentInfo.ConnectionId,
            IpAddress = agentInfo.IpAddress,
            TcpPort = agentInfo.TcpPort ?? 8888 // Default 8888 if null
        };
    }
}




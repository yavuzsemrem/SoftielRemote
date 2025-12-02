namespace SoftielRemote.Backend.Models;

/// <summary>
/// Backend'de tutulan Agent bilgisi.
/// </summary>
public class AgentInfo
{
    /// <summary>
    /// Agent'ın benzersiz Device ID'si.
    /// </summary>
    public string DeviceId { get; set; } = string.Empty;

    /// <summary>
    /// Agent'ın makine adı.
    /// </summary>
    public string MachineName { get; set; } = string.Empty;

    /// <summary>
    /// Agent'ın işletim sistemi bilgisi.
    /// </summary>
    public string OperatingSystem { get; set; } = string.Empty;

    /// <summary>
    /// Agent'ın son görülme zamanı (heartbeat için).
    /// </summary>
    public DateTime LastSeen { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Agent'ın SignalR connection ID'si (varsa).
    /// </summary>
    public string? ConnectionId { get; set; }

    /// <summary>
    /// Agent'ın IP adresi (TCP bağlantısı için).
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// Agent'ın TCP port numarası.
    /// </summary>
    public int TcpPort { get; set; } = 8888;

    /// <summary>
    /// Agent'ın online olup olmadığı.
    /// </summary>
    public bool IsOnline => !string.IsNullOrEmpty(ConnectionId) && 
                           (DateTime.UtcNow - LastSeen).TotalMinutes < 5;
}


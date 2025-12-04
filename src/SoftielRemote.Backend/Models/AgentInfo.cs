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
    /// Agent'ın TCP port numarası. App için null olabilir (App TCP server çalıştırmaz).
    /// </summary>
    public int? TcpPort { get; set; } = 8888;

    /// <summary>
    /// Agent'ın online olup olmadığı.
    /// Faz 1: LastSeen'e göre kontrol (5 dakika içinde heartbeat geldiyse online)
    /// Faz 2: SignalR ConnectionId ile kontrol edilecek
    /// </summary>
    public bool IsOnline
    {
        get
        {
            // Faz 2'de SignalR kullanıldığında ConnectionId kontrolü yapılacak
            if (!string.IsNullOrEmpty(ConnectionId))
            {
                return (DateTime.UtcNow - LastSeen).TotalMinutes < 5;
            }
            
            // Faz 1: LastSeen'e göre kontrol
            // Agent kayıt olduğunda veya heartbeat gönderdiğinde LastSeen güncellenir
            // 5 dakika içinde heartbeat geldiyse online sayılır (30 saniyede bir gönderiliyor)
            // Daha esnek bir süre kullanıyoruz çünkü network gecikmeleri olabilir
            var minutesSinceLastSeen = (DateTime.UtcNow - LastSeen).TotalMinutes;
            return minutesSinceLastSeen < 5;
        }
    }
}


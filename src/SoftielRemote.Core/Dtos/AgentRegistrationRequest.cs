namespace SoftielRemote.Core.Dtos;

/// <summary>
/// Agent'ın Backend'e kayıt olmak için gönderdiği istek.
/// </summary>
public class AgentRegistrationRequest
{
    /// <summary>
    /// Agent'ın benzersiz cihaz ID'si. İlk kayıtta null olabilir, Backend yeni bir ID üretebilir.
    /// </summary>
    public string? DeviceId { get; set; }

    /// <summary>
    /// Agent'ın çalıştığı makinenin adı.
    /// </summary>
    public string? MachineName { get; set; }

    /// <summary>
    /// Agent'ın çalıştığı işletim sistemi bilgisi.
    /// </summary>
    public string? OperatingSystem { get; set; }

    /// <summary>
    /// Agent'ın IP adresi (TCP bağlantısı için).
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// Agent'ın TCP port numarası.
    /// </summary>
    public int TcpPort { get; set; } = 8888;
}


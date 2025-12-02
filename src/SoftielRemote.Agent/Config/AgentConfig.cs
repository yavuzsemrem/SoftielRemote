namespace SoftielRemote.Agent.Config;

/// <summary>
/// Agent yapılandırma ayarları.
/// </summary>
public class AgentConfig
{
    /// <summary>
    /// Backend API base URL'i.
    /// </summary>
    public string BackendBaseUrl { get; set; } = "http://localhost:5056";

    /// <summary>
    /// Agent'ın Device ID'si (kayıt sonrası set edilir).
    /// </summary>
    public string? DeviceId { get; set; }

    /// <summary>
    /// Ekran yakalama için frame interval (milisaniye).
    /// </summary>
    public int FrameIntervalMs { get; set; } = 200;

    /// <summary>
    /// Ekran yakalama çözünürlüğü - Genişlik.
    /// </summary>
    public int ScreenWidth { get; set; } = 800;

    /// <summary>
    /// Ekran yakalama çözünürlüğü - Yükseklik.
    /// </summary>
    public int ScreenHeight { get; set; } = 600;

    /// <summary>
    /// TCP server port (Controller'dan gelen bağlantılar için).
    /// </summary>
    public int TcpServerPort { get; set; } = 8888;
}


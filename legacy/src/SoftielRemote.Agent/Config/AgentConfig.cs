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
    /// Varsayılan: 33ms (30 FPS için).
    /// </summary>
    public int FrameIntervalMs { get; set; } = 33;

    /// <summary>
    /// Ekran yakalama çözünürlüğü - Genişlik.
    /// 0 ise tam ekran çözünürlüğü kullanılır.
    /// </summary>
    public int ScreenWidth { get; set; } = 0;

    /// <summary>
    /// Ekran yakalama çözünürlüğü - Yükseklik.
    /// 0 ise tam ekran çözünürlüğü kullanılır.
    /// </summary>
    public int ScreenHeight { get; set; } = 0;

    /// <summary>
    /// TCP server port (Controller'dan gelen bağlantılar için).
    /// </summary>
    public int TcpServerPort { get; set; } = 8888;

    /// <summary>
    /// TURN sunucu URL'i (WebRTC için, opsiyonel).
    /// </summary>
    public string? TurnServerUrl { get; set; }

    /// <summary>
    /// Video encoding kalite seviyesi (FFmpeg H.264 encoding için).
    /// </summary>
    public Core.Enums.QualityLevel QualityLevel { get; set; } = Core.Enums.QualityLevel.Medium;

    /// <summary>
    /// H.264 encoding kullanılsın mı? (false ise JPEG kullanılır).
    /// </summary>
    public bool UseH264Encoding { get; set; } = false; // Şimdilik false, FFmpeg kurulumu sonrası true yapılabilir
}


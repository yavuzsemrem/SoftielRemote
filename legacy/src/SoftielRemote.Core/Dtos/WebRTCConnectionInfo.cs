namespace SoftielRemote.Core.Dtos;

/// <summary>
/// WebRTC bağlantı bilgisi (kalite metrikleri).
/// </summary>
public class WebRTCConnectionInfo
{
    /// <summary>
    /// Connection ID.
    /// </summary>
    public string ConnectionId { get; set; } = string.Empty;

    /// <summary>
    /// Gecikme (latency) milisaniye cinsinden.
    /// </summary>
    public int LatencyMs { get; set; }

    /// <summary>
    /// Bitrate (bits per second).
    /// </summary>
    public long BitrateBps { get; set; }

    /// <summary>
    /// Frame rate (FPS).
    /// </summary>
    public double FrameRate { get; set; }

    /// <summary>
    /// Paket kaybı yüzdesi.
    /// </summary>
    public double PacketLossPercent { get; set; }

    /// <summary>
    /// Bağlantı kalitesi (0-100 arası).
    /// </summary>
    public int QualityScore { get; set; }
}




namespace SoftielRemote.Core.Messages;

/// <summary>
/// Agent'tan Controller'a gönderilen ekran frame mesajı.
/// </summary>
public class RemoteFrameMessage
{
    /// <summary>
    /// Frame'in genişliği (piksel).
    /// </summary>
    public int Width { get; set; }

    /// <summary>
    /// Frame'in yüksekliği (piksel).
    /// </summary>
    public int Height { get; set; }

    /// <summary>
    /// Sıkıştırılmış görüntü verisi (JPEG/PNG byte array).
    /// </summary>
    public byte[] ImageData { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// Frame'in zaman damgası (UTC).
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Frame sıra numarası (paket kaybı tespiti için).
    /// </summary>
    public long FrameNumber { get; set; }
}


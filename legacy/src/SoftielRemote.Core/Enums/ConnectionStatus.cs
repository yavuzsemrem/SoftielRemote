namespace SoftielRemote.Core.Enums;

/// <summary>
/// Bağlantı durumunu temsil eder.
/// </summary>
public enum ConnectionStatus
{
    /// <summary>
    /// Bağlantı kurulmamış.
    /// </summary>
    Disconnected = 0,

    /// <summary>
    /// Bağlantı isteği gönderildi, bekleniyor.
    /// </summary>
    Pending = 1,

    /// <summary>
    /// Bağlantı kabul edildi, kuruluyor.
    /// </summary>
    Connecting = 2,

    /// <summary>
    /// Bağlantı aktif ve çalışıyor.
    /// </summary>
    Connected = 3,

    /// <summary>
    /// Bağlantı reddedildi.
    /// </summary>
    Rejected = 4,

    /// <summary>
    /// Bağlantı hatası oluştu.
    /// </summary>
    Error = 5
}


using SoftielRemote.Core.Enums;

namespace SoftielRemote.Core.Dtos;

/// <summary>
/// Backend'in bağlantı isteğine verdiği yanıt.
/// </summary>
public class ConnectionResponse
{
    /// <summary>
    /// Bağlantı durumu.
    /// </summary>
    public ConnectionStatus Status { get; set; }

    /// <summary>
    /// İşlem başarılı mı?
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Hata durumunda hata mesajı.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Bağlantı kurulduğunda Agent'ın IP adresi ve port bilgisi (TCP bağlantısı için).
    /// </summary>
    public string? AgentEndpoint { get; set; }

    /// <summary>
    /// Bağlantı ID'si (birden fazla bağlantıyı takip etmek için).
    /// </summary>
    public string? ConnectionId { get; set; }
}


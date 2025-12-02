namespace SoftielRemote.Core.Dtos;

/// <summary>
/// Backend'in Agent kayıt isteğine verdiği yanıt.
/// </summary>
public class AgentRegistrationResponse
{
    /// <summary>
    /// Agent'a atanan benzersiz cihaz ID'si.
    /// </summary>
    public string DeviceId { get; set; } = string.Empty;

    /// <summary>
    /// Kayıt işleminin başarılı olup olmadığı.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Hata durumunda hata mesajı.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Agent'a atanan password (bağlantı için).
    /// </summary>
    public string Password { get; set; } = string.Empty;
}


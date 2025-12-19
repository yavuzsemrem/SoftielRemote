namespace SoftielRemote.Core.Dtos;

/// <summary>
/// Agent keşif yanıtı - Agent'ın hangi Backend'de olduğunu belirtir.
/// </summary>
public class AgentDiscoveryResponse
{
    /// <summary>
    /// Agent bulundu mu?
    /// </summary>
    public bool Found { get; set; }

    /// <summary>
    /// Agent'ın kayıtlı olduğu Backend URL'i (eğer bulunduysa).
    /// </summary>
    public string? BackendUrl { get; set; }

    /// <summary>
    /// Agent'ın online olup olmadığı.
    /// </summary>
    public bool IsOnline { get; set; }

    /// <summary>
    /// Agent bilgileri (opsiyonel).
    /// </summary>
    public string? MachineName { get; set; }
}





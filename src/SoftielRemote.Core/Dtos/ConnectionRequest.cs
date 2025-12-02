namespace SoftielRemote.Core.Dtos;

/// <summary>
/// Controller'ın belirli bir Device ID'ye bağlanmak için gönderdiği istek.
/// </summary>
public class ConnectionRequest
{
    /// <summary>
    /// Bağlanılmak istenen Agent'ın Device ID'si.
    /// </summary>
    public string TargetDeviceId { get; set; } = string.Empty;

    /// <summary>
    /// İsteği gönderen Controller'ın kimliği (opsiyonel, ileride authentication için).
    /// </summary>
    public string? RequesterId { get; set; }

    /// <summary>
    /// Bağlantı için istenen kalite seviyesi.
    /// </summary>
    public Enums.QualityLevel QualityLevel { get; set; } = Enums.QualityLevel.Medium;
}


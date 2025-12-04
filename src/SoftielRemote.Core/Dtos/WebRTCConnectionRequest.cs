using System.ComponentModel.DataAnnotations;
using SoftielRemote.Core.Enums;

namespace SoftielRemote.Core.Dtos;

/// <summary>
/// WebRTC bağlantı isteği (Controller'dan Agent'a).
/// </summary>
public class WebRTCConnectionRequest
{
    /// <summary>
    /// Hedef Agent'ın Device ID'si.
    /// </summary>
    [Required(ErrorMessage = "TargetDeviceId gereklidir")]
    [MaxLength(50, ErrorMessage = "TargetDeviceId maksimum 50 karakter olabilir")]
    [RegularExpression(@"^[0-9]{1,50}$", ErrorMessage = "TargetDeviceId sadece rakamlardan oluşmalıdır")]
    public string TargetDeviceId { get; set; } = string.Empty;

    /// <summary>
    /// İstek yapan Controller'ın Device ID'si.
    /// </summary>
    [Required(ErrorMessage = "RequesterDeviceId gereklidir")]
    [MaxLength(50, ErrorMessage = "RequesterDeviceId maksimum 50 karakter olabilir")]
    [RegularExpression(@"^[0-9]{1,50}$", ErrorMessage = "RequesterDeviceId sadece rakamlardan oluşmalıdır")]
    public string RequesterDeviceId { get; set; } = string.Empty;

    /// <summary>
    /// Connection ID (bağlantı isteğinin benzersiz ID'si).
    /// </summary>
    [Required(ErrorMessage = "ConnectionId gereklidir")]
    [MaxLength(100, ErrorMessage = "ConnectionId maksimum 100 karakter olabilir")]
    public string ConnectionId { get; set; } = string.Empty;

    /// <summary>
    /// İstenen kalite seviyesi.
    /// </summary>
    [Range(0, 3, ErrorMessage = "QualityLevel geçerli bir değer olmalıdır (0=Low, 1=Medium, 2=High, 3=Auto)")]
    public QualityLevel QualityLevel { get; set; } = QualityLevel.Medium;

    /// <summary>
    /// İstenen izin seviyesi.
    /// </summary>
    [Range(0, 1, ErrorMessage = "PermissionLevel geçerli bir değer olmalıdır (0=ViewOnly, 1=FullControl)")]
    public PermissionLevel PermissionLevel { get; set; } = PermissionLevel.FullControl;

    /// <summary>
    /// İstek yapan kişinin adı (opsiyonel).
    /// </summary>
    [MaxLength(255, ErrorMessage = "RequesterName maksimum 255 karakter olabilir")]
    public string? RequesterName { get; set; }
}

/// <summary>
/// WebRTC bağlantı yanıtı (Agent'dan Controller'a).
/// </summary>
public class WebRTCConnectionResponse
{
    /// <summary>
    /// Connection ID.
    /// </summary>
    public string ConnectionId { get; set; } = string.Empty;

    /// <summary>
    /// Bağlantı durumu.
    /// </summary>
    public ConnectionStatus Status { get; set; }

    /// <summary>
    /// Hata mesajı (varsa).
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Bağlantı kabul edildi mi?
    /// </summary>
    public bool Accepted { get; set; }
}




using System.ComponentModel.DataAnnotations;

namespace SoftielRemote.Core.Dtos;

/// <summary>
/// Controller'ın belirli bir Device ID'ye bağlanmak için gönderdiği istek.
/// </summary>
public class ConnectionRequest
{
    /// <summary>
    /// Bağlanılmak istenen Agent'ın Device ID'si.
    /// </summary>
    [Required(ErrorMessage = "TargetDeviceId gereklidir")]
    [MaxLength(50, ErrorMessage = "TargetDeviceId maksimum 50 karakter olabilir")]
    [RegularExpression(@"^[0-9]{1,50}$", ErrorMessage = "TargetDeviceId sadece rakamlardan oluşmalıdır")]
    public string TargetDeviceId { get; set; } = string.Empty;

    /// <summary>
    /// İsteği gönderen Controller'ın kimliği (opsiyonel, ileride authentication için).
    /// </summary>
    [MaxLength(50, ErrorMessage = "RequesterId maksimum 50 karakter olabilir")]
    public string? RequesterId { get; set; }

    /// <summary>
    /// İsteği gönderen Controller'ın makine adı.
    /// </summary>
    [MaxLength(255, ErrorMessage = "RequesterName maksimum 255 karakter olabilir")]
    public string? RequesterName { get; set; }

    /// <summary>
    /// Bağlantı için istenen kalite seviyesi.
    /// </summary>
    [Range(0, 3, ErrorMessage = "QualityLevel geçerli bir değer olmalıdır (0=Low, 1=Medium, 2=High, 3=Auto)")]
    public Enums.QualityLevel QualityLevel { get; set; } = Enums.QualityLevel.Medium;
}


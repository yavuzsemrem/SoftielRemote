using System.ComponentModel.DataAnnotations;

namespace SoftielRemote.Core.Dtos;

/// <summary>
/// Agent'ın heartbeat göndermesi için kullanılan request DTO.
/// </summary>
public class HeartbeatRequest
{
    /// <summary>
    /// Agent'ın Device ID'si.
    /// </summary>
    [Required(ErrorMessage = "DeviceId gereklidir")]
    [MaxLength(50, ErrorMessage = "DeviceId maksimum 50 karakter olabilir")]
    [RegularExpression(@"^[0-9]{1,50}$", ErrorMessage = "DeviceId sadece rakamlardan oluşmalıdır")]
    public string DeviceId { get; set; } = string.Empty;

    /// <summary>
    /// Agent'ın IP adresi (opsiyonel, güncelleme için).
    /// </summary>
    [MaxLength(45, ErrorMessage = "IpAddress maksimum 45 karakter olabilir (IPv4 veya IPv6)")]
    [RegularExpression(@"^(?:(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)$|^([0-9a-fA-F]{1,4}:){7}[0-9a-fA-F]{1,4}$", 
        ErrorMessage = "Geçerli bir IPv4 veya IPv6 adresi giriniz")]
    public string? IpAddress { get; set; }
}


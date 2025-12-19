using System.ComponentModel.DataAnnotations;

namespace SoftielRemote.Core.Dtos;

/// <summary>
/// Agent'ın Backend'e kayıt olmak için gönderdiği istek.
/// </summary>
public class AgentRegistrationRequest
{
    /// <summary>
    /// Agent'ın benzersiz cihaz ID'si. İlk kayıtta null olabilir, Backend yeni bir ID üretebilir.
    /// </summary>
    [MaxLength(50, ErrorMessage = "DeviceId maksimum 50 karakter olabilir")]
    [RegularExpression(@"^[0-9]{1,50}$", ErrorMessage = "DeviceId sadece rakamlardan oluşmalıdır")]
    public string? DeviceId { get; set; }

    /// <summary>
    /// Agent'ın çalıştığı makinenin adı.
    /// </summary>
    [MaxLength(255, ErrorMessage = "MachineName maksimum 255 karakter olabilir")]
    public string? MachineName { get; set; }

    /// <summary>
    /// Agent'ın çalıştığı işletim sistemi bilgisi.
    /// </summary>
    [MaxLength(100, ErrorMessage = "OperatingSystem maksimum 100 karakter olabilir")]
    public string? OperatingSystem { get; set; }

    /// <summary>
    /// Agent'ın IP adresi (TCP bağlantısı için).
    /// </summary>
    [MaxLength(45, ErrorMessage = "IpAddress maksimum 45 karakter olabilir (IPv4 veya IPv6)")]
    [RegularExpression(@"^(?:(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)$|^([0-9a-fA-F]{1,4}:){7}[0-9a-fA-F]{1,4}$", 
        ErrorMessage = "Geçerli bir IPv4 veya IPv6 adresi giriniz")]
    public string? IpAddress { get; set; }

    /// <summary>
    /// Agent'ın TCP port numarası. App için null olabilir (App TCP server çalıştırmaz).
    /// </summary>
    [Range(1, 65535, ErrorMessage = "TcpPort 1 ile 65535 arasında olmalıdır")]
    public int? TcpPort { get; set; } = 8888;
}


using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SoftielRemote.Backend.Models;

/// <summary>
/// Backend kayıt bilgileri (farklı network'lerdeki Backend'lerin keşfi için).
/// </summary>
[Table("BackendRegistry")]
public class BackendRegistryEntity
{
    /// <summary>
    /// Backend'in benzersiz ID'si (Primary Key).
    /// </summary>
    [Key]
    [MaxLength(100)]
    public string BackendId { get; set; } = string.Empty;

    /// <summary>
    /// Backend'in public URL'i (internet üzerinden erişilebilir).
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string PublicUrl { get; set; } = string.Empty;

    /// <summary>
    /// Backend'in local IP adresi (aynı network içinde).
    /// </summary>
    [MaxLength(45)]
    public string? LocalIp { get; set; }

    /// <summary>
    /// Backend'in son görülme zamanı (heartbeat).
    /// </summary>
    public DateTime LastSeen { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Backend'in aktif olup olmadığı.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Backend'in açıklaması (opsiyonel).
    /// </summary>
    [MaxLength(255)]
    public string? Description { get; set; }
}



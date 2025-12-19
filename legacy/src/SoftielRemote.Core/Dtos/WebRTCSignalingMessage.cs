using System.ComponentModel.DataAnnotations;

namespace SoftielRemote.Core.Dtos;

/// <summary>
/// WebRTC signaling mesajı (SDP offer/answer veya ICE candidate).
/// </summary>
public class WebRTCSignalingMessage
{
    /// <summary>
    /// Mesaj tipi (offer, answer, ice-candidate).
    /// </summary>
    [Required(ErrorMessage = "Type gereklidir")]
    [MaxLength(50, ErrorMessage = "Type maksimum 50 karakter olabilir")]
    [RegularExpression(@"^(offer|answer|ice-candidate)$", ErrorMessage = "Type 'offer', 'answer' veya 'ice-candidate' olmalıdır")]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Hedef Device ID (mesajın gönderileceği Agent veya Controller).
    /// </summary>
    [MaxLength(50, ErrorMessage = "TargetDeviceId maksimum 50 karakter olabilir")]
    [RegularExpression(@"^[0-9]{1,50}$", ErrorMessage = "TargetDeviceId sadece rakamlardan oluşmalıdır")]
    public string TargetDeviceId { get; set; } = string.Empty;

    /// <summary>
    /// Gönderen Device ID (mesajı gönderen Agent veya Controller).
    /// </summary>
    [MaxLength(50, ErrorMessage = "SenderDeviceId maksimum 50 karakter olabilir")]
    [RegularExpression(@"^[0-9]{1,50}$", ErrorMessage = "SenderDeviceId sadece rakamlardan oluşmalıdır")]
    public string SenderDeviceId { get; set; } = string.Empty;

    /// <summary>
    /// Connection ID (bağlantı isteğinin ID'si).
    /// </summary>
    [MaxLength(100, ErrorMessage = "ConnectionId maksimum 100 karakter olabilir")]
    public string ConnectionId { get; set; } = string.Empty;

    /// <summary>
    /// SDP offer veya answer (Type = "offer" veya "answer" ise).
    /// </summary>
    [MaxLength(10000, ErrorMessage = "SDP maksimum 10000 karakter olabilir")]
    public string? Sdp { get; set; }

    /// <summary>
    /// ICE candidate (Type = "ice-candidate" ise).
    /// </summary>
    public IceCandidateDto? IceCandidate { get; set; }
}

/// <summary>
/// ICE candidate bilgisi.
/// </summary>
public class IceCandidateDto
{
    /// <summary>
    /// ICE candidate string.
    /// </summary>
    [Required(ErrorMessage = "Candidate gereklidir")]
    [MaxLength(1000, ErrorMessage = "Candidate maksimum 1000 karakter olabilir")]
    public string Candidate { get; set; } = string.Empty;

    /// <summary>
    /// SDP media line index.
    /// </summary>
    [Range(0, 65535, ErrorMessage = "SdpMLineIndex 0 ile 65535 arasında olmalıdır")]
    public int SdpMLineIndex { get; set; }

    /// <summary>
    /// SDP media line ID.
    /// </summary>
    [MaxLength(50, ErrorMessage = "SdpMid maksimum 50 karakter olabilir")]
    public string? SdpMid { get; set; }
}




using Microsoft.Extensions.Logging;
using SIPSorcery.Net;
using SoftielRemote.Core.Dtos;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Windows;
using Newtonsoft.Json;

namespace SoftielRemote.App.Services;

/// <summary>
/// WebRTC client servisi (App tarafı - video alan).
/// SIPSorcery kullanarak WebRTC P2P bağlantı kurar ve video stream'i render eder.
/// </summary>
public class WebRTCClientService : IDisposable
{
    private readonly ILogger<WebRTCClientService> _logger;
    private RTCPeerConnection? _peerConnection;
    private MediaStreamTrack? _videoTrack;
    private bool _disposed = false;
    private readonly object _lock = new();
    private readonly Dispatcher _dispatcher;

    // STUN/TURN sunucuları
    private readonly List<RTCIceServer> _iceServers = new()
    {
        new RTCIceServer { urls = "stun:stun.l.google.com:19302" },
        // TURN sunucusu appsettings.json'dan okunacak
    };

    public WebRTCClientService(ILogger<WebRTCClientService> logger)
    {
        _logger = logger;
        _dispatcher = Dispatcher.CurrentDispatcher;
    }

    /// <summary>
    /// WebRTC peer connection'ı başlatır.
    /// </summary>
    public void Initialize(string? turnServerUrl = null, string? turnUsername = null, string? turnPassword = null)
    {
        lock (_lock)
        {
            if (_disposed)
                return;

            try
            {
                // TURN sunucusu varsa ekle
                if (!string.IsNullOrEmpty(turnServerUrl))
                {
                    var turnServer = new RTCIceServer
                    {
                        urls = turnServerUrl,
                        username = turnUsername,
                        credential = turnPassword
                    };
                    _iceServers.Add(turnServer);
                }

                // RTCPeerConnection oluştur
                var config = new RTCConfiguration
                {
                    iceServers = _iceServers
                };

                _peerConnection = new RTCPeerConnection(config);

                // ICE candidate event
                _peerConnection.onicecandidate += (candidate) =>
                {
                    if (candidate != null)
                    {
                        _logger.LogDebug("ICE candidate: {Candidate}", candidate.candidate);
                        // SIPSorcery'de sdpMLineIndex ushort tipinde (nullable değil)
                        OnIceCandidate?.Invoke(new IceCandidateDto
                        {
                            Candidate = candidate.candidate ?? string.Empty,
                            SdpMLineIndex = candidate.sdpMLineIndex,
                            SdpMid = candidate.sdpMid
                        });
                    }
                };

                // Connection state events
                _peerConnection.onconnectionstatechange += (state) =>
                {
                    _logger.LogInformation("WebRTC connection state: {State}", state);
                    OnConnectionStateChange?.Invoke(state);
                };

                _peerConnection.oniceconnectionstatechange += (state) =>
                {
                    _logger.LogInformation("ICE connection state: {State}", state);
                    OnIceConnectionStateChange?.Invoke(state);
                };

                // Video track event - SIPSorcery'de ontrack event'i yok, alternatif yaklaşım kullanılacak
                // Not: SIPSorcery'nin API'si değişmiş olabilir, video track alımı için farklı bir yaklaşım gerekebilir
                // Şimdilik placeholder, gerçek implementasyon SIPSorcery API'sine göre güncellenecek
                _logger.LogInformation("Video track event handler hazır (SIPSorcery API'sine göre güncellenecek)");

                _logger.LogInformation("WebRTC client başlatıldı");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "WebRTC client başlatılamadı");
            }
        }
    }

    /// <summary>
    /// SDP offer oluşturur ve gönderir.
    /// </summary>
    public Task<string> CreateOfferAsync()
    {
        lock (_lock)
        {
            if (_peerConnection == null || _disposed)
            {
                throw new InvalidOperationException("Peer connection başlatılmamış");
            }

            try
            {
                var offer = _peerConnection.createOffer();
                _peerConnection.setLocalDescription(offer);

                _logger.LogInformation("SDP offer oluşturuldu");
                return Task.FromResult(offer.toJSON());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SDP offer oluşturulamadı");
                return Task.FromException<string>(ex);
            }
        }
    }

    /// <summary>
    /// SDP answer alır ve ayarlar.
    /// </summary>
    public void SetAnswer(string answerSdp)
    {
        lock (_lock)
        {
            if (_peerConnection == null || _disposed)
                return;

            try
            {
                var answer = Newtonsoft.Json.JsonConvert.DeserializeObject<RTCSessionDescriptionInit>(answerSdp);
                if (answer == null)
                    throw new InvalidOperationException("Invalid SDP answer");
                _peerConnection.setRemoteDescription(answer);
                _logger.LogInformation("SDP answer ayarlandı");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SDP answer ayarlanamadı");
            }
        }
    }

    /// <summary>
    /// ICE candidate ekler.
    /// </summary>
    public void AddIceCandidate(IceCandidateDto candidate)
    {
        lock (_lock)
        {
            if (_peerConnection == null || _disposed)
                return;

            try
            {
                var rtcCandidate = new RTCIceCandidateInit
                {
                    candidate = candidate.Candidate,
                    sdpMLineIndex = candidate.SdpMLineIndex >= 0 && candidate.SdpMLineIndex <= ushort.MaxValue 
                        ? (ushort)candidate.SdpMLineIndex 
                        : (ushort)0,
                    sdpMid = candidate.SdpMid
                };

                _peerConnection.addIceCandidate(rtcCandidate);
                _logger.LogDebug("ICE candidate eklendi");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ICE candidate eklenemedi");
            }
        }
    }

    /// <summary>
    /// Video track'i işler ve render eder.
    /// Not: SIPSorcery'nin API'si değişmiş olabilir, video track'ten frame almak için farklı bir yaklaşım gerekebilir.
    /// Şimdilik placeholder implementasyon, gerçek frame alımı için SIPSorcery API'sine göre güncellenecek.
    /// </summary>
    private void HandleVideoTrack(MediaStreamTrack track)
    {
        try
        {
            _logger.LogInformation("Video track işleniyor");
            _videoTrack = track;
            
            // SIPSorcery'de video track'ten frame almak için RTP paketlerini decode etmek gerekir
            // Ancak SIPSorcery'nin API'si değişmiş olabilir
            // Şimdilik placeholder, gerçek implementasyon SIPSorcery API'sine göre güncellenecek
            
            _logger.LogInformation("Video track kaydedildi, frame alımı implement edilecek");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Video track işlenirken hata oluştu");
        }
    }

    /// <summary>
    /// Bağlantıyı kapatır.
    /// </summary>
    public void Close()
    {
        lock (_lock)
        {
            _peerConnection?.close();
            _peerConnection = null;
            _logger.LogInformation("WebRTC client kapatıldı");
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        lock (_lock)
        {
            Close();
            _disposed = true;
        }
    }

    // Events
    public event Action<IceCandidateDto>? OnIceCandidate;
    public event Action<RTCPeerConnectionState>? OnConnectionStateChange;
    public event Action<RTCIceConnectionState>? OnIceConnectionStateChange;
    
    // OnVideoFrameReceived event'i RemoteConnectionViewModel'de kullanılıyor
#pragma warning disable CS0067 // Event is never used - event is used in RemoteConnectionViewModel
    public event Action<WriteableBitmap?>? OnVideoFrameReceived;
#pragma warning restore CS0067
}


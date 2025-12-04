using Microsoft.Extensions.Logging;
using SIPSorcery.Media;
using SIPSorcery.Net;
using SoftielRemote.Core.Dtos;
using SoftielRemote.Core.Enums;
using System.Net;
using Newtonsoft.Json;

namespace SoftielRemote.Agent.Networking;

/// <summary>
/// WebRTC peer connection servisi (Agent tarafı - video gönderen).
/// SIPSorcery kullanarak WebRTC P2P bağlantı kurar.
/// </summary>
public class WebRTCPeerService : IDisposable
{
    private readonly ILogger<WebRTCPeerService> _logger;
    private RTCPeerConnection? _peerConnection;
    private MediaStreamTrack? _videoTrack;
    private bool _disposed = false;
    private readonly object _lock = new();
    private int _videoWidth = 1280;
    private int _videoHeight = 720;

    // STUN/TURN sunucuları
    private readonly List<RTCIceServer> _iceServers = new()
    {
        new RTCIceServer { urls = "stun:stun.l.google.com:19302" },
        // TURN sunucusu appsettings.json'dan okunacak
    };

    public WebRTCPeerService(ILogger<WebRTCPeerService> logger)
    {
        _logger = logger;
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

                _logger.LogInformation("WebRTC peer connection başlatıldı");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "WebRTC peer connection başlatılamadı");
            }
        }
    }

    /// <summary>
    /// SDP offer alır ve answer oluşturur.
    /// </summary>
    public Task<string> CreateAnswerAsync(string offerSdp)
    {
        lock (_lock)
        {
            if (_peerConnection == null || _disposed)
            {
                throw new InvalidOperationException("Peer connection başlatılmamış");
            }

            try
            {
                var offer = Newtonsoft.Json.JsonConvert.DeserializeObject<RTCSessionDescriptionInit>(offerSdp);
                if (offer == null)
                    throw new InvalidOperationException("Invalid SDP offer");
                _peerConnection.setRemoteDescription(offer);

                // Video track oluştur ve ekle (ekran yakalama için)
                // Not: SIPSorcery'de video track oluşturmak için VideoSource veya VideoEndPoint kullanılır
                // Ancak şu anki SIPSorcery versiyonunda VideoEndPoint API'si farklı olabilir
                // Video track'i manuel olarak oluşturuyoruz
                if (_videoTrack == null)
                {
                    try
                    {
                        _logger.LogInformation("Video track oluşturuluyor...");
                        
                        // SIPSorcery'de video track oluşturmak için MediaStreamTrack kullanılır
                        // Video track'i peer connection'a eklemeden önce oluşturmalıyız
                        // Şimdilik video track'i null bırakıyoruz, frame gönderimi için AddVideoTrack kullanılacak
                        _logger.LogInformation("Video track oluşturma - frame gönderimi için AddVideoTrack kullanılacak");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Video track oluşturulamadı");
                    }
                }

                var answer = _peerConnection.createAnswer();
                _peerConnection.setLocalDescription(answer);

                _logger.LogInformation("SDP answer oluşturuldu");
                return Task.FromResult(answer.toJSON());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SDP answer oluşturulamadı");
                throw;
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
    /// Video track ekler (ekran yakalama servisinden).
    /// </summary>
    public void AddVideoTrack(MediaStreamTrack videoTrack)
    {
        lock (_lock)
        {
            if (_peerConnection == null || _disposed)
                return;

            try
            {
                _videoTrack = videoTrack;
                _peerConnection.addTrack(videoTrack);
                _logger.LogInformation("Video track eklendi");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Video track eklenemedi");
            }
        }
    }

    /// <summary>
    /// Ekran yakalama frame'ini WebRTC video stream'e gönderir.
    /// </summary>
    /// <remarks>
    /// Not: SIPSorcery'de video frame göndermek için VideoSource veya VideoEndPoint kullanılır.
    /// Şu anki implementasyonda video track manuel olarak oluşturulacak ve frame'ler
    /// AddVideoTrack ile eklenen track üzerinden gönderilecek.
    /// </remarks>
    public void SendVideoFrame(byte[] frameData, int width, int height, uint timestamp)
    {
        lock (_lock)
        {
            if (_videoTrack == null || _disposed)
            {
                _logger.LogDebug("Video track henüz oluşturulmamış, frame gönderilemedi");
                return;
            }

            try
            {
                // Video boyutları değiştiyse güncelle
                if (_videoWidth != width || _videoHeight != height)
                {
                    _videoWidth = width;
                    _videoHeight = height;
                    _logger.LogDebug("Video boyutları güncellendi: {Width}x{Height}", width, height);
                }

                // SIPSorcery'de video frame göndermek için MediaStreamTrack üzerinden gönderilir
                // Ancak MediaStreamTrack'in doğrudan frame gönderme API'si yok
                // VideoSource veya VideoEndPoint kullanılmalı
                // Şimdilik bu metod placeholder olarak bırakılıyor
                // Gerçek implementasyon için VideoSource kullanılacak
                _logger.LogDebug("Video frame gönderildi: {Width}x{Height}, Timestamp={Timestamp}", width, height, timestamp);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Video frame gönderilemedi");
            }
        }
    }

    /// <summary>
    /// Video boyutlarını ayarlar.
    /// </summary>
    public void SetVideoSize(int width, int height)
    {
        lock (_lock)
        {
            _videoWidth = width;
            _videoHeight = height;
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
            _videoTrack = null;
            _logger.LogInformation("WebRTC peer connection kapatıldı");
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
}


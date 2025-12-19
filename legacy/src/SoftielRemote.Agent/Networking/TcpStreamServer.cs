using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using SoftielRemote.Core.Messages;

namespace SoftielRemote.Agent.Networking;

/// <summary>
/// TCP üzerinden Controller'dan gelen bağlantıları dinleyen ve frame gönderen server.
/// </summary>
public class TcpStreamServer
{
    private TcpListener? _listener;
    private TcpClient? _currentClient;
    private NetworkStream? _currentStream;
    private readonly ILogger<TcpStreamServer> _logger;
    private readonly int _port;
    private readonly System.Threading.ManualResetEventSlim _approvalEvent = new(false);
    private bool _waitingForApproval = false;

    /// <summary>
    /// Client bağlandığında tetiklenen event.
    /// </summary>
    public event Action<string>? OnClientConnected;

    public TcpStreamServer(int port, ILogger<TcpStreamServer> logger)
    {
        _port = port;
        _logger = logger;
    }

    /// <summary>
    /// TCP server'ı başlatır.
    /// </summary>
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _listener = new TcpListener(IPAddress.Any, _port);
            _listener.Start();
            _logger.LogInformation("TCP Server başlatıldı. Port: {Port}", _port);

            // Bağlantı kabul etme döngüsü (onay beklemeli)
            _ = Task.Run(async () =>
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        // İlk bağlantıyı bekle
                        _logger.LogInformation("Bağlantı bekleniyor...");
                        var pendingClient = await _listener.AcceptTcpClientAsync(cancellationToken);
                        
                        // Onay bekleniyor mu kontrol et
                        if (_waitingForApproval)
                        {
                            _logger.LogInformation("Bağlantı geldi, onay bekleniyor...");
                            
                            // Onay verilene kadar bekle (maksimum 60 saniye)
                            if (_approvalEvent.Wait(TimeSpan.FromSeconds(60), cancellationToken))
                            {
                                // Onay verildi - bağlantıyı kabul et
                                _currentClient = pendingClient;
                                _currentStream = _currentClient.GetStream();
                                
                                // TCP stream'i non-blocking yap (önemli!)
                                _currentStream.ReadTimeout = 100; // 100ms timeout
                                _currentStream.WriteTimeout = 5000; // 5 saniye timeout
                                
                                var clientEndPoint = _currentClient.Client.RemoteEndPoint?.ToString() ?? "Unknown";
                                _logger.LogInformation("✅ Client bağlandı (onay verildi): {EndPoint}", clientEndPoint);
                                
                                // Client bağlantı event'ini tetikle
                                try
                                {
                                    OnClientConnected?.Invoke(clientEndPoint);
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogError(ex, "Client bağlantı event'i tetiklenirken hata oluştu");
                                }
                                
                                // Onay event'ini reset et (bir sonraki bağlantı için)
                                _approvalEvent.Reset();
                                _waitingForApproval = false;
                            }
                            else
                            {
                                // Timeout - bağlantıyı reddet
                                _logger.LogWarning("Onay zaman aşımına uğradı, bağlantı reddediliyor");
                                pendingClient.Close();
                                _approvalEvent.Reset();
                                _waitingForApproval = false;
                            }
                        }
                        else
                        {
                            // Onay beklenmiyor - direkt kabul et (eski davranış, backward compatibility)
                            _currentClient = pendingClient;
                            _currentStream = _currentClient.GetStream();
                            
                            _currentStream.ReadTimeout = 100;
                            _currentStream.WriteTimeout = 5000;
                            
                            var clientEndPoint = _currentClient.Client.RemoteEndPoint?.ToString() ?? "Unknown";
                            _logger.LogInformation("Client bağlandı (onay beklenmedi): {EndPoint}", clientEndPoint);
                            
                            try
                            {
                                OnClientConnected?.Invoke(clientEndPoint);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Client bağlantı event'i tetiklenirken hata oluştu");
                            }
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "TCP bağlantı kabul hatası");
                    }
                }
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "TCP Server başlatma hatası");
            throw;
        }
    }

    /// <summary>
    /// Onay beklemeye başlar (connection request geldiğinde çağrılır).
    /// </summary>
    public void WaitForApproval()
    {
        _waitingForApproval = true;
        _approvalEvent.Reset();
        _logger.LogInformation("TCP Server onay bekliyor...");
    }

    /// <summary>
    /// Onay verir (connection request kabul edildiğinde çağrılır).
    /// </summary>
    public void ApproveConnection()
    {
        if (_waitingForApproval)
        {
            _approvalEvent.Set();
            _logger.LogInformation("✅ TCP Server onayı verildi, bağlantı kabul edilecek");
        }
    }

    /// <summary>
    /// Onayı reddeder (connection request reddedildiğinde çağrılır).
    /// </summary>
    public void RejectConnection()
    {
        if (_waitingForApproval)
        {
            _waitingForApproval = false;
            _approvalEvent.Reset();
            _logger.LogInformation("❌ TCP Server onayı reddedildi");
        }
    }

    /// <summary>
    /// Frame'i bağlı client'a gönderir.
    /// </summary>
    public async Task SendFrameAsync(RemoteFrameMessage frame, CancellationToken cancellationToken = default)
    {
        if (_currentStream == null || _currentClient?.Connected != true)
        {
            _logger.LogWarning("Client bağlı değil, frame gönderilemedi. Stream={Stream}, Connected={Connected}", 
                _currentStream != null, _currentClient?.Connected ?? false);
            return;
        }

        try
        {
            // Frame'i JSON olarak serialize et
            var json = JsonSerializer.Serialize(frame);
            var data = System.Text.Encoding.UTF8.GetBytes(json);
            
            // İlk 5 frame için log, sonra her 30 frame'de bir
            if (frame.FrameNumber <= 5 || frame.FrameNumber % 30 == 0)
            {
                _logger.LogInformation("🔵 Frame gönderiliyor: Width={Width}, Height={Height}, DataLength={DataLength}, JsonLength={JsonLength}, FrameNumber={FrameNumber}", 
                    frame.Width, frame.Height, frame.ImageData?.Length ?? 0, data.Length, frame.FrameNumber);
            }
            
            // Önce data uzunluğunu gönder (4 byte)
            var lengthBytes = BitConverter.GetBytes(data.Length);
            await _currentStream.WriteAsync(lengthBytes, 0, 4, cancellationToken);
            
            // Sonra data'yı gönder
            await _currentStream.WriteAsync(data, 0, data.Length, cancellationToken);
            await _currentStream.FlushAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Frame gönderme hatası");
            // Bağlantıyı kapat
            await StopAsync();
        }
    }

    /// <summary>
    /// Client'tan gelen input mesajlarını okur (non-blocking, timeout ile).
    /// </summary>
    public async Task<RemoteInputMessage?> ReceiveInputAsync(CancellationToken cancellationToken = default)
    {
        if (_currentStream == null || _currentClient?.Connected != true)
        {
            return null;
        }

        try
        {
            // Stream'in data available olup olmadığını kontrol et (non-blocking)
            if (!_currentStream.DataAvailable)
            {
                return null; // Data yok, hemen dön (blocking yapma)
            }

            // Data uzunluğunu oku (4 byte) - timeout ile
            var lengthBytes = new byte[4];
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(10); // 10ms timeout - blocking'i önle
            
            try
            {
                var bytesRead = await _currentStream.ReadAsync(lengthBytes, 0, 4, cts.Token);
                
                if (bytesRead != 4)
                {
                    return null;
                }
            }
            catch (OperationCanceledException)
            {
                // Timeout - normal, data yok
                return null;
            }

            var dataLength = BitConverter.ToInt32(lengthBytes, 0);
            
            // Data'yı oku
            var data = new byte[dataLength];
            var totalRead = 0;
            cts.CancelAfter(100); // 100ms timeout
            
            try
            {
                while (totalRead < dataLength)
                {
                    var read = await _currentStream.ReadAsync(data, totalRead, dataLength - totalRead, cts.Token);
                    if (read == 0)
                    {
                        return null;
                    }
                    totalRead += read;
                }
            }
            catch (OperationCanceledException)
            {
                // Timeout - data tam okunamadı
                return null;
            }

            // JSON'u deserialize et
            var json = System.Text.Encoding.UTF8.GetString(data);
            var inputMessage = JsonSerializer.Deserialize<RemoteInputMessage>(json);
            
            return inputMessage;
        }
        catch (Exception ex)
        {
            // Hata durumunda null döndür, frame gönderimini engelleme
            _logger.LogDebug(ex, "Input okuma hatası (normal, data yoksa)");
            return null;
        }
    }

    /// <summary>
    /// Server'ı durdurur.
    /// </summary>
    public async Task StopAsync()
    {
        _currentStream?.Close();
        _currentClient?.Close();
        _listener?.Stop();
        
        _logger.LogInformation("TCP Server durduruldu");
        
        await Task.CompletedTask;
    }

    public bool IsClientConnected => _currentClient?.Connected == true;
}


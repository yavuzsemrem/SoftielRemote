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

            // İlk bağlantıyı bekle
            _logger.LogInformation("Bağlantı bekleniyor...");
            _currentClient = await _listener.AcceptTcpClientAsync(cancellationToken);
            _currentStream = _currentClient.GetStream();
            _logger.LogInformation("Client bağlandı: {EndPoint}", _currentClient.Client.RemoteEndPoint);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "TCP Server başlatma hatası");
            throw;
        }
    }

    /// <summary>
    /// Frame'i bağlı client'a gönderir.
    /// </summary>
    public async Task SendFrameAsync(RemoteFrameMessage frame, CancellationToken cancellationToken = default)
    {
        if (_currentStream == null || !_currentClient?.Connected == true)
        {
            _logger.LogWarning("Client bağlı değil, frame gönderilemedi");
            return;
        }

        try
        {
            // Frame'i JSON olarak serialize et
            var json = JsonSerializer.Serialize(frame);
            var data = System.Text.Encoding.UTF8.GetBytes(json);
            
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
    /// Client'tan gelen input mesajlarını okur.
    /// </summary>
    public async Task<RemoteInputMessage?> ReceiveInputAsync(CancellationToken cancellationToken = default)
    {
        if (_currentStream == null || _currentClient?.Connected != true)
        {
            return null;
        }

        try
        {
            // Data uzunluğunu oku (4 byte)
            var lengthBytes = new byte[4];
            var bytesRead = await _currentStream.ReadAsync(lengthBytes, 0, 4, cancellationToken);
            
            if (bytesRead != 4)
            {
                return null;
            }

            var dataLength = BitConverter.ToInt32(lengthBytes, 0);
            
            // Data'yı oku
            var data = new byte[dataLength];
            var totalRead = 0;
            while (totalRead < dataLength)
            {
                var read = await _currentStream.ReadAsync(data, totalRead, dataLength - totalRead, cancellationToken);
                if (read == 0)
                {
                    return null;
                }
                totalRead += read;
            }

            // JSON'u deserialize et
            var json = System.Text.Encoding.UTF8.GetString(data);
            var inputMessage = JsonSerializer.Deserialize<RemoteInputMessage>(json);
            
            return inputMessage;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Input okuma hatası");
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


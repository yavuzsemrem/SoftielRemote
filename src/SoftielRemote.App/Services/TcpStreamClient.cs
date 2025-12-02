using System.Net.Sockets;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using SoftielRemote.Core.Messages;

namespace SoftielRemote.App.Services;

/// <summary>
/// TCP üzerinden Agent'a bağlanan ve frame alan client implementasyonu.
/// </summary>
public class TcpStreamClient : ITcpStreamClient
{
    private TcpClient? _tcpClient;
    private NetworkStream? _stream;
    private readonly ILogger<TcpStreamClient>? _logger;

    public bool IsConnected => _tcpClient?.Connected == true;

    public TcpStreamClient(ILogger<TcpStreamClient>? logger = null)
    {
        _logger = logger;
    }

    public async Task<bool> ConnectAsync(string host, int port)
    {
        try
        {
            _tcpClient = new TcpClient();
            await _tcpClient.ConnectAsync(host, port);
            _stream = _tcpClient.GetStream();
            
            _logger?.LogInformation("Agent'a bağlanıldı: {Host}:{Port}", host, port);
            return true;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Bağlantı hatası: {Host}:{Port}", host, port);
            Disconnect();
            return false;
        }
    }

    public async Task<RemoteFrameMessage?> ReceiveFrameAsync(CancellationToken cancellationToken = default)
    {
        if (_stream == null || !IsConnected)
        {
            return null;
        }

        try
        {
            // Data uzunluğunu oku (4 byte)
            var lengthBytes = new byte[4];
            var bytesRead = await _stream.ReadAsync(lengthBytes, 0, 4, cancellationToken);
            
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
                var read = await _stream.ReadAsync(data, totalRead, dataLength - totalRead, cancellationToken);
                if (read == 0)
                {
                    return null;
                }
                totalRead += read;
            }

            // JSON'u deserialize et
            var json = System.Text.Encoding.UTF8.GetString(data);
            var frame = JsonSerializer.Deserialize<RemoteFrameMessage>(json);
            
            return frame;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Frame alma hatası");
            return null;
        }
    }

    public async Task SendInputAsync(RemoteInputMessage input)
    {
        if (_stream == null || !IsConnected)
        {
            return;
        }

        try
        {
            var json = JsonSerializer.Serialize(input);
            var data = System.Text.Encoding.UTF8.GetBytes(json);
            
            // Önce data uzunluğunu gönder (4 byte)
            var lengthBytes = BitConverter.GetBytes(data.Length);
            await _stream.WriteAsync(lengthBytes, 0, 4);
            
            // Sonra data'yı gönder
            await _stream.WriteAsync(data, 0, data.Length);
            await _stream.FlushAsync();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Input gönderme hatası");
        }
    }

    public void Disconnect()
    {
        _stream?.Close();
        _tcpClient?.Close();
        _tcpClient = null;
        _stream = null;
        _logger?.LogInformation("Bağlantı kapatıldı");
    }
}


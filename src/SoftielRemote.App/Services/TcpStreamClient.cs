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
            System.Diagnostics.Debug.WriteLine($"🔵 TcpStreamClient.ConnectAsync başlatılıyor: {host}:{port}");
            _tcpClient = new TcpClient();
            
            // Timeout ayarla (10 saniye)
            var connectTask = _tcpClient.ConnectAsync(host, port);
            var timeoutTask = Task.Delay(TimeSpan.FromSeconds(10));
            var completedTask = await Task.WhenAny(connectTask, timeoutTask);
            
            if (completedTask == timeoutTask)
            {
                System.Diagnostics.Debug.WriteLine($"❌ TCP bağlantı timeout: {host}:{port}");
                Disconnect();
                return false;
            }
            
            await connectTask;
            _stream = _tcpClient.GetStream();
            
            System.Diagnostics.Debug.WriteLine($"✅ Agent'a bağlanıldı: {host}:{port}");
            _logger?.LogInformation("Agent'a bağlanıldı: {Host}:{Port}", host, port);
            return true;
        }
        catch (System.Net.Sockets.SocketException ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ SocketException: {ex.SocketErrorCode} - {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"❌ Host: {host}, Port: {port}");
            _logger?.LogError(ex, "Bağlantı hatası: {Host}:{Port}", host, port);
            Disconnect();
            return false;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ TCP bağlantı exception: {ex.GetType().Name} - {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"❌ Host: {host}, Port: {port}");
            _logger?.LogError(ex, "Bağlantı hatası: {Host}:{Port}", host, port);
            Disconnect();
            return false;
        }
    }

    public async Task<RemoteFrameMessage?> ReceiveFrameAsync(CancellationToken cancellationToken = default)
    {
        if (_stream == null || !IsConnected)
        {
            System.Diagnostics.Debug.WriteLine("⚠️ TcpStreamClient: Stream null veya bağlı değil");
            return null;
        }

        try
        {
            // Data uzunluğunu oku (4 byte)
            var lengthBytes = new byte[4];
            var bytesRead = await _stream.ReadAsync(lengthBytes, 0, 4, cancellationToken);
            
            System.Diagnostics.Debug.WriteLine($"🔵 TcpStreamClient: Length bytes okundu: {bytesRead}/4");
            
            if (bytesRead != 4)
            {
                if (bytesRead == 0)
                {
                    System.Diagnostics.Debug.WriteLine("⚠️ TcpStreamClient: Bağlantı kapatıldı (0 byte okundu)");
                }
                return null;
            }

            var dataLength = BitConverter.ToInt32(lengthBytes, 0);
            System.Diagnostics.Debug.WriteLine($"🔵 TcpStreamClient: Data uzunluğu: {dataLength} bytes");
            
            // Data'yı oku
            var data = new byte[dataLength];
            var totalRead = 0;
            while (totalRead < dataLength)
            {
                var read = await _stream.ReadAsync(data, totalRead, dataLength - totalRead, cancellationToken);
                if (read == 0)
                {
                    System.Diagnostics.Debug.WriteLine($"⚠️ TcpStreamClient: Data okuma sırasında bağlantı kapatıldı ({totalRead}/{dataLength} okundu)");
                    return null;
                }
                totalRead += read;
            }

            System.Diagnostics.Debug.WriteLine($"✅ TcpStreamClient: Tüm data okundu: {totalRead} bytes");

            // JSON'u deserialize et
            var json = System.Text.Encoding.UTF8.GetString(data);
            System.Diagnostics.Debug.WriteLine($"🔵 TcpStreamClient: JSON uzunluğu: {json.Length} karakter");
            var frame = JsonSerializer.Deserialize<RemoteFrameMessage>(json);
            
            if (frame == null)
            {
                System.Diagnostics.Debug.WriteLine("❌ TcpStreamClient: Frame deserialize edilemedi (null)");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"✅ TcpStreamClient: Frame deserialize edildi: Width={frame.Width}, Height={frame.Height}");
            }
            
            return frame;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ TcpStreamClient exception: {ex.GetType().Name} - {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"❌ StackTrace: {ex.StackTrace}");
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


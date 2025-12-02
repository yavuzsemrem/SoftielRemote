using SoftielRemote.Core.Messages;

namespace SoftielRemote.App.Services;

/// <summary>
/// TCP üzerinden Agent'a bağlanan ve frame alan client interface'i.
/// </summary>
public interface ITcpStreamClient
{
    /// <summary>
    /// Agent'a bağlanır.
    /// </summary>
    Task<bool> ConnectAsync(string host, int port);

    /// <summary>
    /// Frame alır (blocking).
    /// </summary>
    Task<RemoteFrameMessage?> ReceiveFrameAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Input mesajı gönderir.
    /// </summary>
    Task SendInputAsync(RemoteInputMessage input);

    /// <summary>
    /// Bağlantıyı kapatır.
    /// </summary>
    void Disconnect();

    /// <summary>
    /// Bağlı mı?
    /// </summary>
    bool IsConnected { get; }
}


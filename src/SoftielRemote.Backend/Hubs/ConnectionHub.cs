using Microsoft.AspNetCore.SignalR;

namespace SoftielRemote.Backend.Hubs;

/// <summary>
/// SignalR Hub - Agent ve Controller arasında signaling için.
/// Faz 2'de kullanılacak, şimdilik temel yapı.
/// </summary>
public class ConnectionHub : Hub
{
    private readonly ILogger<ConnectionHub> _logger;

    public ConnectionHub(ILogger<ConnectionHub> logger)
    {
        _logger = logger;
    }

    public override Task OnConnectedAsync()
    {
        _logger.LogInformation("Client bağlandı: {ConnectionId}", Context.ConnectionId);
        return base.OnConnectedAsync();
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("Client bağlantısı kesildi: {ConnectionId}", Context.ConnectionId);
        return base.OnDisconnectedAsync(exception);
    }
}


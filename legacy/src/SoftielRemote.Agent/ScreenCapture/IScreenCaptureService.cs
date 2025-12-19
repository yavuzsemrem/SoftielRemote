using SoftielRemote.Core.Messages;

namespace SoftielRemote.Agent.ScreenCapture;

/// <summary>
/// Ekran yakalama servisi interface'i.
/// </summary>
public interface IScreenCaptureService
{
    /// <summary>
    /// Ekran görüntüsünü yakalar ve RemoteFrameMessage olarak döndürür.
    /// </summary>
    Task<RemoteFrameMessage?> CaptureScreenAsync(int width, int height);
}


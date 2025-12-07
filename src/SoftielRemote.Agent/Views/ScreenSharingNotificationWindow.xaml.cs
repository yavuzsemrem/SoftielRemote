using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace SoftielRemote.Agent.Views;

/// <summary>
/// Ekran paylaşımı bildirimi için toast notification window.
/// </summary>
public partial class ScreenSharingNotificationWindow : Window
{
    private DispatcherTimer? _autoCloseTimer;

    public ScreenSharingNotificationWindow()
    {
        InitializeComponent();
        Loaded += ScreenSharingNotificationWindow_Loaded;
    }

    private void ScreenSharingNotificationWindow_Loaded(object sender, RoutedEventArgs e)
    {
        // Fade-in animasyonu
        var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.3));
        BeginAnimation(UIElement.OpacityProperty, fadeIn);

        // 5 saniye sonra otomatik kapat
        _autoCloseTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(5)
        };
        _autoCloseTimer.Tick += (s, args) =>
        {
            _autoCloseTimer.Stop();
            CloseWithFadeOut();
        };
        _autoCloseTimer.Start();
    }

    private void CloseWithFadeOut()
    {
        var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromSeconds(0.3));
        fadeOut.Completed += (s, e) => Close();
        BeginAnimation(UIElement.OpacityProperty, fadeOut);
    }

    protected override void OnClosed(EventArgs e)
    {
        _autoCloseTimer?.Stop();
        base.OnClosed(e);
    }
}






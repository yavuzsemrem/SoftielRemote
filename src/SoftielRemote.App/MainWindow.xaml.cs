using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Runtime.InteropServices;
using SoftielRemote.App.Services;
using SoftielRemote.App.ViewModels;
using SoftielRemote.Core.Utils;

namespace SoftielRemote.App;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        try
    {
        InitializeComponent();
            
            // Backend URL'ini appsettings.json'dan oku
            var configuredBackendUrl = ReadBackendUrlFromConfig();
            
            // Loaded event'inde async olarak backend URL'ini çöz
            Loaded += async (s, e) =>
            {
                await ResolveAndInitializeBackendAsync(configuredBackendUrl);
            };
            
            // Geçici olarak varsayılan URL ile başlat (async çözümleme tamamlanana kadar)
            var tempBackendClient = new BackendClientService(configuredBackendUrl ?? "http://localhost:5000");
            var tcpClient = new Services.TcpStreamClient(null);
            DataContext = new MainViewModel(tempBackendClient, tcpClient);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"MainWindow oluşturulurken hata: {ex.Message}\n\nStack Trace: {ex.StackTrace}", 
                "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            throw;
        }
    }

    /// <summary>
    /// appsettings.json'dan Backend URL'ini okur.
    /// </summary>
    private string? ReadBackendUrlFromConfig()
    {
            try
            {
                var configPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
                if (System.IO.File.Exists(configPath))
                {
                    var json = System.IO.File.ReadAllText(configPath);
                    var config = System.Text.Json.JsonSerializer.Deserialize<System.Collections.Generic.Dictionary<string, object>>(json);
                    if (config != null && config.ContainsKey("BackendBaseUrl"))
                    {
                    return config["BackendBaseUrl"]?.ToString();
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"⚠️ appsettings.json okunamadı: {ex.Message}");
        }
        return null;
    }

    /// <summary>
    /// Backend URL'ini çözer ve ViewModel'i günceller.
    /// </summary>
    private async Task ResolveAndInitializeBackendAsync(string? configuredUrl)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"🔵 Backend URL çözülüyor... (Configured: {configuredUrl ?? "null"})");
            
            var resolvedUrl = await BackendUrlResolver.ResolveBackendUrlAsync(configuredUrl);
            
            if (resolvedUrl == null)
            {
                System.Diagnostics.Debug.WriteLine("⚠️ Backend URL bulunamadı. appsettings.json'da BackendBaseUrl belirtin veya Backend'i başlatın.");
                resolvedUrl = configuredUrl ?? "http://localhost:5000"; // Fallback
                            }
                            else
                            {
                System.Diagnostics.Debug.WriteLine($"✅ Backend URL bulundu: {resolvedUrl}");
                
                // Bulunan URL'i appsettings.json'a kaydet (eğer farklıysa)
                if (resolvedUrl != configuredUrl)
                {
                    SaveBackendUrlToConfig(resolvedUrl);
                }
            }
            
            // ViewModel'i yeni URL ile güncelle
            if (DataContext is MainViewModel viewModel)
            {
                var newBackendClient = new BackendClientService(resolvedUrl);
                viewModel.UpdateBackendClient(newBackendClient);
                System.Diagnostics.Debug.WriteLine($"🔵 ViewModel Backend URL güncellendi: {resolvedUrl}");
                }
            }
            catch (Exception ex)
            {
            System.Diagnostics.Debug.WriteLine($"❌ Backend URL çözülürken hata: {ex.Message}");
            }
    }

    /// <summary>
    /// Backend URL'ini appsettings.json'a kaydeder.
    /// </summary>
    private void SaveBackendUrlToConfig(string url)
    {
        try
        {
            var configPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
            var config = new System.Collections.Generic.Dictionary<string, object>();
            
            if (System.IO.File.Exists(configPath))
            {
                var json = System.IO.File.ReadAllText(configPath);
                config = System.Text.Json.JsonSerializer.Deserialize<System.Collections.Generic.Dictionary<string, object>>(json) ?? config;
            }
            
            config["BackendBaseUrl"] = url;
            var options = new System.Text.Json.JsonSerializerOptions { WriteIndented = true };
            System.IO.File.WriteAllText(configPath, System.Text.Json.JsonSerializer.Serialize(config, options));
            
            System.Diagnostics.Debug.WriteLine($"💾 Backend URL appsettings.json'a kaydedildi: {url}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"⚠️ Backend URL kaydedilemedi: {ex.Message}");
        }
    }

    private void MinimizeButton_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void MaximizeButton_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        
        // Full screen'de border'ı koru
        if (WindowState == WindowState.Maximized)
        {
            WindowBorder.BorderThickness = new Thickness(1);
        }
    }
    
    protected override void OnStateChanged(EventArgs e)
    {
        base.OnStateChanged(e);
        
        // Full screen'de border'ı koru
        if (WindowState == WindowState.Maximized)
        {
            WindowBorder.BorderThickness = new Thickness(1);
        }
    }
    
    private bool _isResizing = false;
    private System.Windows.Point _resizeStartPoint;
    private WindowResizeDirection _resizeDirection;
    private double _resizeStartWidth;
    private double _resizeStartHeight;
    private double _resizeStartLeft;
    private double _resizeStartTop;

    private void ResizeWindow_Start(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
        {
            System.Diagnostics.Debug.WriteLine("Resize başlatıldı!");
            _isResizing = true;
            _resizeStartPoint = e.GetPosition(this);
            _resizeStartWidth = Width;
            _resizeStartHeight = Height;
            _resizeStartLeft = Left;
            _resizeStartTop = Top;
            
            // Hangi yönde resize yapılacağını belirle
            if (sender is System.Windows.Controls.Border border)
            {
                System.Diagnostics.Debug.WriteLine($"Resize handle: {border.Name}");
                if (border.Name == "ResizeHandleTopLeft")
                    _resizeDirection = WindowResizeDirection.TopLeft;
                else if (border.Name == "ResizeHandleTopRight")
                    _resizeDirection = WindowResizeDirection.TopRight;
                else if (border.Name == "ResizeHandleBottomRight")
                    _resizeDirection = WindowResizeDirection.BottomRight;
                else if (border.Name == "ResizeHandleBottomLeft")
                    _resizeDirection = WindowResizeDirection.BottomLeft;
                else if (border.Name == "ResizeHandleTop")
                    _resizeDirection = WindowResizeDirection.Top;
                else if (border.Name == "ResizeHandleBottom")
                    _resizeDirection = WindowResizeDirection.Bottom;
                else if (border.Name == "ResizeHandleLeft")
                    _resizeDirection = WindowResizeDirection.Left;
                else if (border.Name == "ResizeHandleRight")
                    _resizeDirection = WindowResizeDirection.Right;
            }
            
            // Window seviyesinde mouse capture - mouse pencere dışına çıksa bile takip et
            CaptureMouse();
            e.Handled = true;
        }
    }

    private void Window_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (_isResizing)
        {
            // Mouse pozisyonunu pencere koordinatlarında al
            var currentPoint = e.GetPosition(this);
            
            var deltaX = currentPoint.X - _resizeStartPoint.X;
            var deltaY = currentPoint.Y - _resizeStartPoint.Y;

            switch (_resizeDirection)
            {
                case WindowResizeDirection.TopLeft:
                    Width = Math.Max(MinWidth, _resizeStartWidth - deltaX);
                    Height = Math.Max(MinHeight, _resizeStartHeight - deltaY);
                    Left = _resizeStartLeft + (_resizeStartWidth - Width);
                    Top = _resizeStartTop + (_resizeStartHeight - Height);
                    break;
                case WindowResizeDirection.TopRight:
                    Width = Math.Max(MinWidth, _resizeStartWidth + deltaX);
                    Height = Math.Max(MinHeight, _resizeStartHeight - deltaY);
                    Top = _resizeStartTop + (_resizeStartHeight - Height);
                    break;
                case WindowResizeDirection.BottomRight:
                    Width = Math.Max(MinWidth, _resizeStartWidth + deltaX);
                    Height = Math.Max(MinHeight, _resizeStartHeight + deltaY);
                    break;
                case WindowResizeDirection.BottomLeft:
                    Width = Math.Max(MinWidth, _resizeStartWidth - deltaX);
                    Height = Math.Max(MinHeight, _resizeStartHeight + deltaY);
                    Left = _resizeStartLeft + (_resizeStartWidth - Width);
                    break;
                case WindowResizeDirection.Top:
                    Height = Math.Max(MinHeight, _resizeStartHeight - deltaY);
                    Top = _resizeStartTop + (_resizeStartHeight - Height);
                    break;
                case WindowResizeDirection.Bottom:
                    Height = Math.Max(MinHeight, _resizeStartHeight + deltaY);
                    break;
                case WindowResizeDirection.Left:
                    Width = Math.Max(MinWidth, _resizeStartWidth - deltaX);
                    Left = _resizeStartLeft + (_resizeStartWidth - Width);
                    break;
                case WindowResizeDirection.Right:
                    Width = Math.Max(MinWidth, _resizeStartWidth + deltaX);
                    break;
            }
        }
    }

    private void Window_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (_isResizing)
        {
            _isResizing = false;
            ReleaseMouseCapture();
            e.Handled = true;
        }
    }
    
    private enum WindowResizeDirection
    {
        Left = 1,
        Right = 2,
        Top = 3,
        TopLeft = 4,
        TopRight = 5,
        Bottom = 6,
        BottomLeft = 7,
        BottomRight = 8
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void TitleBar_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            // Double click to maximize/restore
            MaximizeButton_Click(sender, e);
        }
        else if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
        {
            // Drag window
            DragMove();
        }
    }

    private void WindowControl_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (sender is System.Windows.Controls.Button button)
        {
            button.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(50, 255, 255, 255));
        }
    }

    private void WindowControl_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (sender is System.Windows.Controls.Button button)
        {
            button.Background = System.Windows.Media.Brushes.Transparent;
        }
    }

    private void CloseButton_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (sender is System.Windows.Controls.Button button)
        {
            button.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(232, 17, 35));
        }
    }

    private void CloseButton_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (sender is System.Windows.Controls.Button button)
        {
            button.Background = System.Windows.Media.Brushes.Transparent;
        }
    }

    private void NavTabButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // Reset all nav tabs
            var transparentBrush = System.Windows.Media.Brushes.Transparent;
            var inactiveForeground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(148, 163, 184));
            
            NewConnectionTab.Background = transparentBrush;
            NewConnectionTab.Foreground = inactiveForeground;
            AddDeviceTab.Background = transparentBrush;
            AddDeviceTab.Foreground = inactiveForeground;
            AddressBookTab.Background = transparentBrush;
            AddressBookTab.Foreground = inactiveForeground;

            // Set active tab
            var button = sender as System.Windows.Controls.Button;
            if (button != null)
            {
                // Mavi gradient brush oluştur
                var gradientBrush = new System.Windows.Media.LinearGradientBrush
                {
                    StartPoint = new System.Windows.Point(0, 0),
                    EndPoint = new System.Windows.Point(1, 0)
                };
                gradientBrush.GradientStops.Add(new System.Windows.Media.GradientStop(
                    System.Windows.Media.Color.FromRgb(0, 168, 255), 0.0));
                gradientBrush.GradientStops.Add(new System.Windows.Media.GradientStop(
                    System.Windows.Media.Color.FromRgb(0, 102, 255), 0.5));
                gradientBrush.GradientStops.Add(new System.Windows.Media.GradientStop(
                    System.Windows.Media.Color.FromRgb(0, 85, 255), 1.0));
                
                button.Background = gradientBrush;
                button.Foreground = System.Windows.Media.Brushes.White;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Nav tab button click error: {ex.Message}");
        }
    }

    private void TabButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // Reset all tabs
            var transparentBrush = System.Windows.Media.Brushes.Transparent;
            var inactiveForeground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(148, 163, 184));
            
            NewsTab.Background = transparentBrush;
            NewsTab.Foreground = inactiveForeground;
            FavoritesTab.Background = transparentBrush;
            FavoritesTab.Foreground = inactiveForeground;
            RecentSessionsTab.Background = transparentBrush;
            RecentSessionsTab.Foreground = inactiveForeground;
            DiscoveredTab.Background = transparentBrush;
            DiscoveredTab.Foreground = inactiveForeground;
            InvitationsTab.Background = transparentBrush;
            InvitationsTab.Foreground = inactiveForeground;

            // Set active tab
            var button = sender as System.Windows.Controls.Button;
            if (button != null)
            {
                // Mavi gradient brush oluştur
                var gradientBrush = new System.Windows.Media.LinearGradientBrush
                {
                    StartPoint = new System.Windows.Point(0, 0),
                    EndPoint = new System.Windows.Point(1, 0)
                };
                gradientBrush.GradientStops.Add(new System.Windows.Media.GradientStop(
                    System.Windows.Media.Color.FromRgb(0, 168, 255), 0.0));
                gradientBrush.GradientStops.Add(new System.Windows.Media.GradientStop(
                    System.Windows.Media.Color.FromRgb(0, 102, 255), 0.5));
                gradientBrush.GradientStops.Add(new System.Windows.Media.GradientStop(
                    System.Windows.Media.Color.FromRgb(0, 85, 255), 1.0));
                
                button.Background = gradientBrush;
                button.Foreground = System.Windows.Media.Brushes.White;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Tab button click error: {ex.Message}");
        }
    }

    private void ShowAllSessions_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        // Show all sessions logic
    }

    private void SessionCard_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        // Connect to session logic
        var border = sender as System.Windows.Controls.Border;
        if (border != null)
        {
            // Find TextBlock in the Border's visual tree
            var textBlocks = new List<System.Windows.Controls.TextBlock>();
            FindVisualChildren<System.Windows.Controls.TextBlock>(border, textBlocks);
            
            // Get the first TextBlock that contains a Device ID (9 digits)
            var deviceIdTextBlock = textBlocks.FirstOrDefault(tb => 
                !string.IsNullOrEmpty(tb.Text) && 
                tb.Text.Length == 9 && 
                tb.Text.All(char.IsDigit));
            
            if (deviceIdTextBlock != null && !string.IsNullOrEmpty(deviceIdTextBlock.Text))
            {
                var viewModel = DataContext as MainViewModel;
                viewModel?.ConnectToDevice(deviceIdTextBlock.Text);
            }
        }
    }

    private void FindVisualChildren<T>(System.Windows.DependencyObject parent, List<T> results) where T : System.Windows.DependencyObject
    {
        for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
            if (child is T t)
                results.Add(t);
            FindVisualChildren<T>(child, results);
        }
    }

    private T? FindVisualChild<T>(System.Windows.DependencyObject parent) where T : System.Windows.DependencyObject
    {
        for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
            if (child is T)
                return (T)child;
            var childOfChild = FindVisualChild<T>(child);
            if (childOfChild != null)
                return childOfChild;
        }
        return null;
    }

    private void CopyDeviceId_Click(object sender, RoutedEventArgs e)
    {
        CopyDeviceIdToClipboard();
    }

    private void DeviceIdTextBlock_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        CopyDeviceIdToClipboard();
    }

    private void CopyDeviceIdToClipboard()
    {
        try
        {
            var viewModel = DataContext as MainViewModel;
            if (viewModel != null && !string.IsNullOrEmpty(viewModel.YourDeviceId) && 
                viewModel.YourDeviceId != "---" && viewModel.YourDeviceId != "Bağlanamadı" && 
                viewModel.YourDeviceId != "Hata")
            {
                // Orijinal ID'yi kopyala (boşluksuz)
                Clipboard.SetText(viewModel.YourDeviceId);
                
                // Animasyonlu başarı göstergesi göster
                ShowSuccessAnimation(DeviceIdSuccessIndicator);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Kopyalama hatası: {ex.Message}");
        }
    }

    private void CopyPassword_Click(object sender, RoutedEventArgs e)
    {
        CopyPasswordToClipboard();
    }

    private void PasswordTextBlock_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        CopyPasswordToClipboard();
    }

    private void CopyPasswordToClipboard()
    {
        try
        {
            var viewModel = DataContext as MainViewModel;
            if (viewModel != null && !string.IsNullOrEmpty(viewModel.Password) && viewModel.Password != "---")
            {
                Clipboard.SetText(viewModel.Password);
                
                // Animasyonlu başarı göstergesi göster
                ShowSuccessAnimation(PasswordSuccessIndicator);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Kopyalama hatası: {ex.Message}");
        }
    }

    /// <summary>
    /// Modern animasyonlu başarı göstergesi gösterir
    /// </summary>
    private async void ShowSuccessAnimation(System.Windows.Controls.Border successIndicator)
    {
        if (successIndicator == null) return;

        // RenderTransform'i ayarla
        successIndicator.RenderTransform = new System.Windows.Media.ScaleTransform();
        successIndicator.RenderTransformOrigin = new System.Windows.Point(0.5, 0.5);

        // Görünür yap
        successIndicator.Visibility = System.Windows.Visibility.Visible;
        successIndicator.Opacity = 0;

        // Animasyon oluştur
        var scaleXAnimation = new System.Windows.Media.Animation.DoubleAnimation
        {
            From = 0,
            To = 1.2,
            Duration = new System.Windows.Duration(TimeSpan.FromMilliseconds(150)),
            EasingFunction = new System.Windows.Media.Animation.ElasticEase
            {
                Oscillations = 0,
                Springiness = 3
            }
        };

        var scaleYAnimation = new System.Windows.Media.Animation.DoubleAnimation
        {
            From = 0,
            To = 1.2,
            Duration = new System.Windows.Duration(TimeSpan.FromMilliseconds(150)),
            EasingFunction = new System.Windows.Media.Animation.ElasticEase
            {
                Oscillations = 0,
                Springiness = 3
            }
        };

        var opacityAnimation = new System.Windows.Media.Animation.DoubleAnimation
        {
            From = 0,
            To = 1,
            Duration = new System.Windows.Duration(TimeSpan.FromMilliseconds(100))
        };

        // Scale animasyonu
        var scaleTransform = (System.Windows.Media.ScaleTransform)successIndicator.RenderTransform;
        scaleTransform.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleXProperty, scaleXAnimation);
        scaleTransform.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleYProperty, scaleYAnimation);
        successIndicator.BeginAnimation(System.Windows.UIElement.OpacityProperty, opacityAnimation);

        // 1.2 saniye bekle
        await Task.Delay(1200);

        // Küçülme animasyonu
        var scaleDownXAnimation = new System.Windows.Media.Animation.DoubleAnimation
        {
            From = 1.2,
            To = 1,
            Duration = new System.Windows.Duration(TimeSpan.FromMilliseconds(100))
        };

        var scaleDownYAnimation = new System.Windows.Media.Animation.DoubleAnimation
        {
            From = 1.2,
            To = 1,
            Duration = new System.Windows.Duration(TimeSpan.FromMilliseconds(100))
        };

        scaleTransform.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleXProperty, scaleDownXAnimation);
        scaleTransform.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleYProperty, scaleDownYAnimation);

        // Fade out animasyonu
        await Task.Delay(200);
        var fadeOutAnimation = new System.Windows.Media.Animation.DoubleAnimation
        {
            From = 1,
            To = 0,
            Duration = new System.Windows.Duration(TimeSpan.FromMilliseconds(300))
        };

        fadeOutAnimation.Completed += (s, e) =>
        {
            successIndicator.Visibility = System.Windows.Visibility.Collapsed;
        };

        successIndicator.BeginAnimation(System.Windows.UIElement.OpacityProperty, fadeOutAnimation);
    }

    private void RemoteAddressTextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == System.Windows.Input.Key.Enter)
        {
            System.Diagnostics.Debug.WriteLine("🔵 Enter tuşuna basıldı, bağlantı başlatılıyor...");
            var viewModel = DataContext as MainViewModel;
            if (viewModel != null)
            {
                // TextBox'taki değeri ViewModel'e aktar (binding gecikmesi olabilir)
                var textBox = sender as System.Windows.Controls.TextBox;
                if (textBox != null && !string.IsNullOrWhiteSpace(textBox.Text))
                {
                    viewModel.RemoteDeviceId = textBox.Text;
                    System.Diagnostics.Debug.WriteLine($"🔵 RemoteDeviceId güncellendi: {viewModel.RemoteDeviceId}");
                }
                
                // ConnectCommand'i tetikle
                if (viewModel.ConnectCommand.CanExecute(null))
                {
                    System.Diagnostics.Debug.WriteLine("✅ ConnectCommand çalıştırılıyor...");
                    viewModel.ConnectCommand.Execute(null);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"⚠️ ConnectCommand çalıştırılamıyor (CanExecute = false). IsConnected: {viewModel.IsConnected}, RemoteDeviceId: '{viewModel.RemoteDeviceId}'");
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("❌ ViewModel null!");
            }
            e.Handled = true;
        }
    }
}

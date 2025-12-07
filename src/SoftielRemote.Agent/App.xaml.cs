using System.Windows;

namespace SoftielRemote.Agent;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private static App? _instance;
    
    public static App? Instance => _instance;

    public App()
    {
        // Constructor'da instance'ı set et (Application_Startup event'inden önce)
        // Bu sayede app.Run() çağrılmadan önce de instance mevcut olur
        _instance = this;
        System.Diagnostics.Debug.WriteLine("✅ WPF Application instance set edildi (constructor)");
        Console.WriteLine("✅ WPF Application instance set edildi (constructor)");
    }

    private void Application_Startup(object sender, StartupEventArgs e)
    {
        // WPF Application başlatıldı, ama window açmıyoruz
        // Sadece popup'lar için kullanılacak
        // Instance zaten constructor'da set edildi, burada sadece loglama yapıyoruz
        System.Diagnostics.Debug.WriteLine("✅ WPF Application başlatıldı (Application_Startup)");
        Console.WriteLine("✅ WPF Application başlatıldı (Application_Startup)");
    }
}


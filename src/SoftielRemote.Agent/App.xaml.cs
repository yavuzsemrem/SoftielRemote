using System.Windows;

namespace SoftielRemote.Agent;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private void Application_Startup(object sender, StartupEventArgs e)
    {
        // WPF Application başlatıldı, ama window açmıyoruz
        // Sadece popup'lar için kullanılacak
    }
}


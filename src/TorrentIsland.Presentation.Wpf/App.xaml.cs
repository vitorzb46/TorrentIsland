using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TorrentIsland.Infrastructure.DependencyInjection;
using TorrentIsland.Presentation.Wpf.Logging;
using System.Windows;

namespace TorrentIsland.Presentation.Wpf;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : System.Windows.Application
{
    private IHost? _host;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var builder = Host.CreateApplicationBuilder(e.Args);
        builder.Services.AddTorrentIsland(builder.Configuration, maxLogs: 50);

        builder.Services.AddSingleton<WpfLogSink>();
        builder.Services.AddSingleton<MainWindow>();

        _host = builder.Build();

        var mainWindow = _host.Services.GetRequiredService<MainWindow>();
        MainWindow = mainWindow;
        mainWindow.Show();
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        if (_host is IAsyncDisposable disposable)
        {
            await disposable.DisposeAsync().ConfigureAwait(false);
        }

        base.OnExit(e);
    }
}


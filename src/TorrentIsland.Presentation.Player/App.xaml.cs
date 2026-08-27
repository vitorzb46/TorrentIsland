using System.IO;
using System.Windows;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MonoTorrent.Client;
using TorrentIsland.Application.Contracts;
using TorrentIsland.Application.Interfaces;
using TorrentIsland.Application.Medias.Commands;
using TorrentIsland.Application.Services;
using TorrentIsland.Application.Settings;
using TorrentIsland.Domain.Interfaces;
using TorrentIsland.Infrastructure.DependencyInjection;
using TorrentIsland.Infrastructure.Interfaces;
using TorrentIsland.Infrastructure.Logging;
using TorrentIsland.Infrastructure.MonoTorrent;
using TorrentIsland.Infrastructure.Services;

namespace TorrentIsland.Presentation.Player;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : System.Windows.Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Logs limpos a cada execução para o timeline não misturar sessões.
        foreach (var arquivo in new[] { "player-debug.log", "vlc-errors.log" })
        {
            try
            {
                var caminho = Path.Combine(AppContext.BaseDirectory, arquivo);
                if (File.Exists(caminho)) File.Delete(caminho);
            }
            catch { /* best-effort */ }
        }

        // Uso: TorrentIsland.Presentation.Player.exe <url-do-stream>
        var mediaUrl = e.Args.Length > 0 ? e.Args[0] : null;
        
        var services = new ServiceCollection();
        ConfigurarServico(services);
        var serviceProvider = services.BuildServiceProvider();

        var window = serviceProvider.GetRequiredService<PlayerWindow>();
        window.mediaUrl = mediaUrl!;
        window.Show();
    }

    private void ConfigurarServico(IServiceCollection services)
    {
        var configuration = new ConfigurationBuilder()
        .SetBasePath(AppContext.BaseDirectory)
        .Build();
        var consoleLog = services.BuildServiceProvider().GetRequiredService<IConsoleLogRenderer>();

        services.AddTorrentIsland(configuration, consoleLog, maxLogs: 10);
        services.AddSingleton<PlayerWindow>();
    }
}
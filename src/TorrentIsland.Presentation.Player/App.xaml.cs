using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.IO;
using System.Windows;
using TorrentIsland.Application.Interfaces;
using TorrentIsland.Application.Services;
using TorrentIsland.Infrastructure.DependencyInjection;
using TorrentIsland.Infrastructure.Interfaces;
using TorrentIsland.Presentation.Console.Helpers;
using TorrentIsland.Presentation.Console.Renderers;

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
        Task.Delay(1000).Wait(); // Aguarda 1 segundo para mostrar a janela | OBS: Tem algum bug que abre o programa sem abrir a janela, resolver depois.
        window.Show();
    }

    private void ConfigurarServico(IServiceCollection services)
    {
        var configuration = new ConfigurationBuilder()
        .SetBasePath(AppContext.BaseDirectory)
        .Build();
        services.AddSingleton<ILogPainel, LogPainel>();
        services.AddSingleton<IConsoleLogRenderer, ConsoleLogRenderer>();
        var consoleLog = services.BuildServiceProvider().GetRequiredService<IConsoleLogRenderer>();

        services.AddTorrentIsland(configuration, consoleLog, maxLogs: 10);
        services.AddSingleton<IFormattingHelper, FormattingHelper>();
        services.AddSingleton<IStreamService, StreamService>();
        services.AddSingleton<PlayerWindow>();
    }
}
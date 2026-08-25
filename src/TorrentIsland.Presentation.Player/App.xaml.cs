using System.IO;
using System.Windows;

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
        // O FullUri vem do StreamResult gerado pelo TorrentService (streaming do MonoTorrent).
        var mediaUrl = e.Args.Length > 0 ? e.Args[0] : null;
        if (string.IsNullOrWhiteSpace(mediaUrl))
        {
            MessageBox.Show("Uso: TorrentIsland.Presentation.Player.exe <url-do-stream>", "Player",
                MessageBoxButton.OK, MessageBoxImage.Information);
            //Shutdown(1);
            //return;
        }

        var window = new PlayerWindow(mediaUrl!);
        window.Show();
    }
}
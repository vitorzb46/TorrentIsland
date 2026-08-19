using System.Diagnostics;
using System.IO;
using System.Windows;
using TorrentIsland.Presentation.Wpf.Logging;

namespace TorrentIsland.Presentation.Wpf;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{

    public MainWindow(WpfLogSink sink)
    {
        InitializeComponent();
        LogsListBox.ItemsSource = sink.Entries;
    }

    private async void Streamar_Click(object sender, RoutedEventArgs e)
    {
        var magnet = MagnetTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(magnet))
        {
            MessageBox.Show("Informe um magnet link.", "Torrent Island", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        StreamarButton.IsEnabled = false;
        try
        {
            // O Uri devolvido é a URL HTTP servida pelo MonoTorrent para este torrent.
            //var uri = await _downloadService.StreamAsync(magnet);
            //AbrirNoPlayer(uri.ToString());
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Falha ao iniciar o streaming:\n{ex.Message}", "Torrent Island",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            StreamarButton.IsEnabled = true;
        }
    }

    private void AbrirNoPlayer(string fullUri)
    {
        var playerExe = Path.Combine(AppContext.BaseDirectory, "TorrentIsland.Presentation.Player.exe");
        if (!File.Exists(playerExe))
        {
            MessageBox.Show($"Player não encontrado:\n{playerExe}", "Torrent Island",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var psi = new ProcessStartInfo(playerExe) { UseShellExecute = false };
        psi.ArgumentList.Add(fullUri);
        Process.Start(psi);
    }
}
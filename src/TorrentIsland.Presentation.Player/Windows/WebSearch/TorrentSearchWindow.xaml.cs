using System.Windows;
using System.Windows.Input;
using TorrentIsland.Presentation.Player.DTOs;
using static TorrentIsland.Presentation.Player.Scripts.Limao;

namespace TorrentIsland.Presentation.Player.Windows.WebSearch;

public partial class TorrentSearchWindow : Wpf.Ui.Controls.FluentWindow
{
    public string? LinkSelecionado { get; private set; }

    public TorrentSearchWindow()
    {
        InitializeComponent();
    }

    private async void BtnBuscar_Click(object sender, RoutedEventArgs e)
    {
        string query = TxtSearch.Text.Trim();
        if (string.IsNullOrEmpty(query)) return;

        TxtSearch.IsEnabled = false;
        BtnBuscar.IsEnabled = false;
        try
        {
            var resultado = await SearchAsync(query);
            ListBoxTorrents.ItemsSource = resultado;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erro na busca: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            TxtSearch.IsEnabled = true;
            BtnBuscar.IsEnabled = true;
        }
    }

    private async void ListBoxTorrents_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        await SelectedTorrent();
    }
    
    private void TxtSearch_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            BtnBuscar_Click(sender, e);
            e.Handled = true;
        }
    }

    private async void ListBoxTorrents_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            await SelectedTorrent();
            e.Handled = true;
        }
    }

    private async Task SelectedTorrent()
    {
        if (ListBoxTorrents.SelectedItem is TorrentSearchDto torrent)
        {
            var magnet = await GetUrlMagneticAsync(torrent.TorrentName);
            LinkSelecionado = magnet;

            DialogResult = true;
            Close();
        }
    }

    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            DialogResult = false;
            Close();
            e.Handled = true;
        }
    }
}
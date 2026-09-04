using System.Windows;
using System.Windows.Input;
using TorrentIsland.Presentation.Player.Scripts;
using static TorrentIsland.Presentation.Player.Scripts.Limao;

namespace TorrentIsland.Presentation.Player;

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

    private void ListBoxTorrents_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        SelectedTorrent();
    }
    
    private void TxtSearch_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            BtnBuscar_Click(sender, e);
            e.Handled = true;
        }
    }

    private void ListBoxTorrents_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            SelectedTorrent();
            e.Handled = true;
        }
    }

    private void SelectedTorrent()
    {
        if (ListBoxTorrents.SelectedItem is TorrentSearchDto torrent)
        {
            LinkSelecionado = torrent.LinkDownload;

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
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TorrentIsland.Application.Contracts;
using TorrentIsland.Application.DTOs;
using TorrentIsland.Application.Interfaces;
using TorrentIsland.Infrastructure.Logging;
using TorrentIsland.Presentation.Player.ViewModel;
using static TorrentIsland.Presentation.Player.Scripts.Limao;

namespace TorrentIsland.Presentation.Player.Windows.WebSearch;

public partial class TorrentSearchWindow : Wpf.Ui.Controls.FluentWindow
{
    private readonly ITorrentService _torrent;
    private readonly ITorrentStatusEvent _statusEvent;

    public string? LinkSelecionado { get; private set; }

    public TorrentSearchWindow(PlayerViewModel viewModel, ITorrentService torrent, ITorrentStatusEvent statusEvent)
    {
        InitializeComponent();
        TorrentDownloadQueue();
        DataContext = viewModel;
        ListBoxProgresso.ItemsSource = viewModel.TorrentDownloads;
        _torrent = torrent;
        _statusEvent = statusEvent;
    }
    
    private async void BtnBuscar_Click(object sender, RoutedEventArgs e)
    {
        var query = TxtSearch.Text.Trim();
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

    private void BtnAlternarTela_Click(object sender, RoutedEventArgs e)
    {
        if (PainelBusca.Visibility == Visibility.Visible)
        {
            Log.Salvar("_statusEvent iniciou!");
            _statusEvent.Start();
            PainelBusca.Visibility = Visibility.Collapsed;
            PainelProgresso.Visibility = Visibility.Visible;
            
            BtnAlternarTela.Content = "Voltar para Busca"; 
        }
        else
        {
            Log.Salvar("_statusEvent parou!");
            // _statusEvent.Stop();
            PainelBusca.Visibility = Visibility.Visible;
            PainelProgresso.Visibility = Visibility.Collapsed;
            
            BtnAlternarTela.Content = "Downloads";
        }
    }

    private async void Add_Lista_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem { Parent: ContextMenu contextMenu })
        {
            if (contextMenu.PlacementTarget is ListBoxItem item)
            {
                var data = item.DataContext as TorrentSearchDto;
                if (data != null)
                {
                    try
                    {
                        var magnet = await GetUrlMagneticAsync(data.TorrentName);
                        // Baixar torrent
                        await _torrent.CriarTorrentAsync(magnet);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Erro na busca: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }
        e.Handled = true;
    }

    private void TorrentDownloadQueue()
    {
        var menuContexto = new ContextMenu();
        var itemMenu = new MenuItem { Header = "Adicionar à lista" };
        itemMenu.Click += Add_Lista_Click;
        menuContexto.Items.Add(itemMenu);

        var estiloItem = new Style(typeof(ListBoxItem));
        estiloItem.Setters.Add(new Setter(ContextMenuProperty, menuContexto));
        estiloItem.Setters.Add(new Setter(HorizontalContentAlignmentProperty, HorizontalAlignment.Stretch));
        estiloItem.Setters.Add(new Setter(VerticalContentAlignmentProperty, VerticalAlignment.Center));

        ListBoxTorrents.ItemContainerStyle = estiloItem;
    }
}
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TorrentIsland.Application.DTOs;
using TorrentIsland.Application.Interfaces;
using TorrentIsland.Presentation.Player.ViewModel;
using TorrentIsland.Presentation.Player.Windows.Main;
using static TorrentIsland.Presentation.Player.Scripts.Limao;

namespace TorrentIsland.Presentation.Player.Windows.WebSearch;

public partial class TorrentSearchWindow : Wpf.Ui.Controls.FluentWindow
{
    private readonly PlayerWindow _playerWindow;
    private readonly ITorrentService _torrent;
    private readonly ITorrentStatusEvent _statusEvent;

    public string? LinkSelecionado { get; private set; }

    public TorrentSearchWindow(
        PlayerViewModel viewModel,
        PlayerWindow playerWindow,
        ITorrentService torrent,
        ITorrentStatusEvent statusEvent)
    {
        InitializeComponent();
        TorrentSearch_ContextMenu();
        DonwloadProgress_ConextMenu();
        DataContext = viewModel;
        ListBoxProgresso.ItemsSource = viewModel.TorrentDownloads;
        _playerWindow = playerWindow;
        _torrent = torrent;
        _statusEvent = statusEvent;
        Closed += (_, _) => _statusEvent.Stop();
    }

    #region Mouse Click
    private async void ListBoxTorrents_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        await SelectedTorrent();
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

    private void BtnAlternarTela_Click(object sender, RoutedEventArgs e)
    {
        if (PainelBusca.Visibility == Visibility.Visible)
        {
            _statusEvent.Start();
            PainelBusca.Visibility = Visibility.Collapsed;
            PainelProgresso.Visibility = Visibility.Visible;

            BtnAlternarTela.Content = "Voltar para Busca";
        }
        else
        {
            _statusEvent.Stop();
            PainelBusca.Visibility = Visibility.Visible;
            PainelProgresso.Visibility = Visibility.Collapsed;

            BtnAlternarTela.Content = "Downloads";
        }
    }

    private async void Add_Lista_Click(object sender, RoutedEventArgs e)
    {
        await ExecutarTorrentAsync(sender, e, _torrent.CriarTorrentAsync).ConfigureAwait(false);
    }

    private async void Start_Stream_Click(object sender, RoutedEventArgs e)
    {
        await ExecutarTorrentAsync(sender, e, async magnet =>
        {
            await _playerWindow.CarregarStreamTorrentAsync(magnet)
                                       .ContinueWith(_ => Dispatcher.Invoke(() => Close()))
                                       .ConfigureAwait(false);
        }).ConfigureAwait(false);
    }

    private async void Iniciar_Download_Click(object sender, RoutedEventArgs e)
    {
        if (ListBoxProgresso.SelectedItem is TorrentDownloadDto selecionado)
        {
            await _torrent.IniciarAsync(selecionado.TorrentId);
            e.Handled = true;
        }
    }

    private async void Parar_Download_Click(object sender, RoutedEventArgs e)
    {
        if (ListBoxProgresso.SelectedItem is TorrentDownloadDto selecionado)
        {
            await _torrent.PararAsync(selecionado.TorrentId);
            e.Handled = true;
        }
    }

    private async void Pausar_Download_Click(object sender, RoutedEventArgs e)
    {
        if (ListBoxProgresso.SelectedItem is TorrentDownloadDto selecionado)
        {
            await _torrent.PausarAsync(selecionado.TorrentId);
            e.Handled = true;
        }
    }

    private async void Remover_da_Lista_Click(object sender, RoutedEventArgs e)
    {
        if (ListBoxProgresso.SelectedItem is not TorrentDownloadDto selecionado)
            return;

        if (DataContext is PlayerViewModel vm)
        {
            await _torrent.RemoverAsync(selecionado.TorrentId);
            vm.TorrentDownloads.Remove(selecionado);
            e.Handled = true;
        }
    }
    #endregion

    #region Key Press
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

    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            DialogResult = false;
            Close();
            e.Handled = true;
        }
    }
    #endregion

    #region Context Menu
    private void TorrentSearch_ContextMenu()
    {
        var menuContexto = new ContextMenu();
        var stream = new MenuItem { Header = "Iniciar Stream" };
        var itemMenu = new MenuItem { Header = "Adicionar à lista" };
        stream.Click += Start_Stream_Click;
        itemMenu.Click += Add_Lista_Click;
        menuContexto.Items.Add(stream);
        menuContexto.Items.Add(itemMenu);
        ContextMenuStyle(menuContexto, ListBoxTorrents);
    }

    private void DonwloadProgress_ConextMenu()
    {
        var menuContexto = new ContextMenu();
        var iniciar = new MenuItem { Header = "Iniciar / Resumir" };
        var parar = new MenuItem { Header = "Parar" };
        var pausar = new MenuItem { Header = "Pausar" };
        var remover = new MenuItem { Header = "Remover da lista" };
        iniciar.Click += Iniciar_Download_Click;
        parar.Click += Parar_Download_Click;
        pausar.Click += Pausar_Download_Click;
        remover.Click += Remover_da_Lista_Click;
        menuContexto.Items.Add(iniciar);
        menuContexto.Items.Add(parar);
        menuContexto.Items.Add(pausar);
        menuContexto.Items.Add(remover);
        ContextMenuStyle(menuContexto, ListBoxProgresso);
    }
    #endregion

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

    private static void ContextMenuStyle(ContextMenu menuContexto, ListBox listBox)
    {
        var estiloItem = new Style(typeof(ListBoxItem));
        estiloItem.Setters.Add(new Setter(ContextMenuProperty, menuContexto));
        estiloItem.Setters.Add(new Setter(HorizontalContentAlignmentProperty, HorizontalAlignment.Stretch));
        estiloItem.Setters.Add(new Setter(VerticalContentAlignmentProperty, VerticalAlignment.Center));
        listBox.ItemContainerStyle = estiloItem;
    }

    private static async Task ExecutarTorrentAsync(object sender, RoutedEventArgs e, Func<string, Task> executar)
    {
        if (sender is MenuItem { Parent: ContextMenu { PlacementTarget: ListBoxItem item } })
        {
            var data = item.DataContext as TorrentSearchDto;
            if (data != null)
            {
                try
                {
                    var magnet = await GetUrlMagneticAsync(data.TorrentName);

                    await executar(magnet).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erro: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        e.Handled = true;
    }
}
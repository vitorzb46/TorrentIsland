using LibVLCSharp.Shared;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using TorrentIsland.Application.Interfaces;
using TorrentIsland.Infrastructure.Interfaces;
using TorrentIsland.Infrastructure.VLC;
namespace TorrentIsland.Presentation.Player;

public partial class PlayerWindow : Wpf.Ui.Controls.FluentWindow
{
    private readonly PlayerViewModel _viewModel;
    private readonly ControlsWindow _controls;
    private readonly DispatcherTimer _inactivityTimer;
    public string mediaUrl { get; set; } = "";
    private IStreamService StreamService { get; }
    private IManagers Managers { get; }

    public PlayerWindow(IStreamService streamService, IManagers managers)
    {
        Log.Salvar($"PlayerWindow ctor | mediaUrl={mediaUrl}");
        InitializeComponent();

        var vlc = new VlcPlayerService();

        _viewModel = new PlayerViewModel(vlc.LibVLC, vlc.MediaPlayer);

        DataContext = _viewModel;

        VideoView.MediaPlayer = vlc.MediaPlayer;
        Log.Salvar("MediaPlayer associado ao VideoView");

        // Janela de controles separada (evita o airspace do HWND nativo bloquear os cliques).
        // Owner é atribuído no Loaded (a janela dona precisa estar visível antes).
        _controls = new ControlsWindow(_viewModel, this);
        _controls.FullscreenRequested += (_, _) => ToggleFullscreen();
        _controls.ActivityDetected += (_, _) => ReiniciarTimerInatividade();
        _controls.Closed += (_, _) => Close();

        _inactivityTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(3)
        };
        _inactivityTimer.Tick += (_, _) => HideControls();

        Loaded += (_, _) =>
        {
            Log.Salvar("Loaded disparado — posicionando controles");
            // Owner garante que a janela de controles fique sempre à frente do player.
            _controls.Owner = this;

            // --- RESOLUÇÃO DO BUG DO MENU FLUTUANTE (Foco do Windows) ---
            this.Activated += (s, e) => _controls.Topmost = true;
            this.Deactivated += (s, e) => _controls.Topmost = false;

            PosicionarControles();
            _controls.Show();
        };
        Loaded += async (_, _) =>
        {
            if (!string.IsNullOrWhiteSpace(mediaUrl))
            {
                await CarregarMidiaAsync(mediaUrl);
            }
            else
            {
                // Talvez overlay no futuro
                Log.Salvar("Nenhuma mídia inicial fornecida.");
            }
        };
        Closed += (_, _) =>
        {
            Log.Salvar("Window fechada — dispose do ViewModel");
            _inactivityTimer.Stop();
            _controls.Close();
            _viewModel.Dispose();
            vlc.Dispose();
            Environment.Exit(0);
        };

        // Sincroniza a janela de controles com a janela de vídeo.
        LocationChanged += (_, _) => PosicionarControles();
        SizeChanged += (_, _) => PosicionarControles();
        StateChanged += (_, _) => PosicionarControles();
        StreamService = streamService;
        Managers = managers;
    }

    public async Task CarregarMidiaAsync(string caminhoOuUrl)
    {
        try
        {
            Log.Salvar($"Carregando mídia: {caminhoOuUrl}");

            Media media;
            if (File.Exists(caminhoOuUrl))
            {
                Log.Salvar($"Arquivo existe. Tamanho: {Utils.BytesFormat} bytes");
                media = new Media(_viewModel.LibVLC, caminhoOuUrl, FromType.FromPath);
                _viewModel.MidiaFilePath = caminhoOuUrl;
            }
            else if (Uri.TryCreate(caminhoOuUrl, UriKind.Absolute, out _))
            {
                Log.Salvar("Origem é uma URL");
                media = new Media(_viewModel.LibVLC, caminhoOuUrl, FromType.FromLocation);
            }
            else
            {
                MessageBox.Show("Caminho de mídia inválido.", "Player", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Retiro da thread principal se causar algum deadlock, até agora normal.
            var torrents = await Managers.ObterTorrentsAsync();
            if (torrents.Count > 0)
            {
                _viewModel.TorrentName = torrents.Select(t => t.Value.Nome).FirstOrDefault()!
                                                                           .Replace("[[", "[").Replace("]]", "]");
            }

            // Opções de rede para streaming
            media.AddOption(":network-caching=3000");
            media.AddOption(":file-caching=3000");
            media.AddOption(":live-caching=3000");
            media.AddOption(":skip-frames");
            media.AddOption(":clock-synchro=0");
            media.AddOption(":clock-jitter=5000");

            _viewModel.SetMedia(media);
            await Utils.AtualizarUIAsync(async () =>
            {
                await _viewModel.PopulateTracksAsync().ConfigureAwait(false);
                ShowControls();
            });
        }
        catch (Exception ex)
        {
            Log.Salvar($"Erro ao carregar mídia: {ex.Message}");
            MessageBox.Show($"Não foi possível carregar a mídia: {caminhoOuUrl}", "Player",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    public async Task CarregarStreamTorrentAsync(string caminhoOuUrl)
    {
        try
        {
            _viewModel.IsLoading = true;
            Log.Salvar($"Iniciando stream de torrent: {caminhoOuUrl}");

            // Tirado da thread principal pois o MonoTorrent causava deadlocks
            // com o ClientEngine usando .GetAwaiter().GetResult() no construtor deles.
            string streamUrl = await Task.Run(async () =>
            {
                return await StreamService.ToPlayerAsync(caminhoOuUrl);
            });

            Log.Salvar($"Stream URL obtida: {streamUrl}");
            await CarregarMidiaAsync(streamUrl);
        }
        catch (Exception ex)
        {
            Log.Salvar($"Erro ao iniciar stream de torrent: {ex.Message}");
            MessageBox.Show($"Não foi possível iniciar o stream do torrent!", "Player",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            _viewModel.IsLoading = false;
        }
    }

    /// <summary>Posiciona a janela de controles na parte inferior da janela de vídeo.</summary>
    private void PosicionarControles()
    {
        if (_controls is null) return;

        // Se estiver em modo cinema, usamos a matemática baseada na tela cheia
        if (_viewModel.IsFullscreen)
        {
            _controls.Width = SystemParameters.PrimaryScreenWidth;
            _controls.Left = 0; // Zera a propriedade esquerda permanentemente na tela cheia
            _controls.Top = SystemParameters.PrimaryScreenHeight - _controls.Height;
        }
        // Janela Maximizada (Botão maximizar do windows (do player))
        else if (this.WindowState == WindowState.Maximized)
        {
            _controls.Width = SystemParameters.WorkArea.Width;
            _controls.Left = SystemParameters.WorkArea.Left;
            _controls.Top = SystemParameters.WorkArea.Bottom - _controls.Height;
        }
        // Se estiver em modo janela normal, usamos a matemática baseada no Player (this)
        else
        {
            _controls.Width = this.ActualWidth;
            _controls.Left = this.Left;
            _controls.Top = this.Top + this.ActualHeight - _controls.Height;
        }
    }

    private void ReiniciarTimerInatividade()
    {
        if (_viewModel.IsFullscreen)
        {
            _inactivityTimer.Stop();
            _inactivityTimer.Start();
        }
    }

    // --- Modo cinema ---
    private void ShowControls()
    {
        if (_controls.Visibility != Visibility.Visible)
        {
            _controls.Visibility = Visibility.Visible;
            Mouse.OverrideCursor = Cursors.Arrow;
        }
        ReiniciarTimerInatividade();
    }

    private void HideControls()
    {
        _inactivityTimer.Stop();

        if (_viewModel.IsFullscreen)
        {
            _controls.Visibility = Visibility.Collapsed;
            Mouse.OverrideCursor = Cursors.None;
        }
    }

    private void ToggleFullscreen()
    {
        if (WindowState == WindowState.Normal)
        {
            // Entra em tela cheia
            WindowStyle = WindowStyle.None;
            WindowState = WindowState.Maximized;
            _viewModel.IsFullscreen = true;

            if (MyTitleBar != null)
            {
                MyTitleBar.Visibility = Visibility.Collapsed;
            }
        }
        else
        {
            // Volta para o modo janela
            WindowStyle = WindowStyle.SingleBorderWindow;
            WindowState = WindowState.Normal;
            _viewModel.IsFullscreen = false;

            if (MyTitleBar != null)
            {
                MyTitleBar.Visibility = Visibility.Visible;
            }
        }

        _viewModel.IsFullscreen = WindowState == WindowState.Maximized;

        // Executa o posicionamento um milissegundo depois, garantindo que o Windows já mudou de tamanho
        Dispatcher.BeginInvoke(new Action(() =>
        {
            PosicionarControles();
            ShowControls();
        }), DispatcherPriority.Render);
    }

    private void Player_Mouse(object sender, MouseEventArgs e)
    {
        // Se estiver em tela cheia, avisa o sistema para resetar o timer de inatividade
        if (_viewModel.IsFullscreen)
        {
            ShowControls();
            MouseDetected?.Invoke(this, EventArgs.Empty);
        }
    }

    private void PlayerWindow_DragEnter(object sender, DragEventArgs e)
    {
        if (Utils.TryGetDataObject(e.Data, DataFormats.FileDrop, out string[] arquivos)
            && Utils.ExtensoesVideo.Contains(Path.GetExtension(arquivos[0]).ToLowerInvariant()))
        {
            e.Effects = DragDropEffects.Copy;
            e.Handled = true;
        }
    }

    private void PlayerWindow_DragOver(object sender, DragEventArgs e)
    {
        if (Utils.TryGetDataObject(e.Data, DataFormats.FileDrop, out string[] arquivos)
            && (Utils.ExtensoesVideo.Contains(arquivos[0]) || Utils.ExtensaoTorrent.Contains(arquivos[0])))
        {
            e.Effects = DragDropEffects.Copy;
            e.Handled = true;
        }
    }

    private async void PlayerWindow_Drop(object sender, DragEventArgs e)
    {
        if (Utils.TryGetDataObject(e.Data, DataFormats.FileDrop, out string[] arquivos))
        {
            if (Utils.ExtensoesVideo.Contains(Path.GetExtension(arquivos[0]).ToLowerInvariant()))
            {
                Log.Salvar($"Arquivo de vídeo colado: {arquivos[0]}");
                await CarregarMidiaAsync(arquivos[0]);
            }
            else if (Utils.ExtensaoTorrent.Contains(Path.GetExtension(arquivos[0]).ToLowerInvariant()))
            {
                Log.Salvar($"Arquivo torrent colado: {arquivos[0]}");
                await CarregarStreamTorrentAsync(arquivos[0]);
            }
            else
            {
                MessageBox.Show("Formato de arquivo não suportado.", "Player",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }

    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        base.OnPreviewKeyDown(e);

        if (e.Key == Key.Escape && _viewModel.IsFullscreen)
        {
            ToggleFullscreen();
        }

        if (e.Key == Key.V && Keyboard.Modifiers == ModifierKeys.Control)
        {
            var dataObject = Clipboard.GetDataObject();
            if (dataObject == null) return;

            if (Utils.TryGetDataObject(dataObject, DataFormats.FileDrop, out string[] arquivos)
                && Utils.ExtensoesVideo.Contains(Path.GetExtension(arquivos[0]).ToLowerInvariant())) // Arquivo das extensões de vídeo suportadas
            {
                e.Handled = true;
                _ = CarregarMidiaAsync(arquivos[0]);
                return;
            }

            if (Utils.TryGetDataObject(dataObject, DataFormats.FileDrop, out string[] torrent)
                && Utils.ExtensaoTorrent.Contains(Path.GetExtension(torrent[0]).ToLowerInvariant())) // Arquivo torrent
            {
                e.Handled = true;
                _ = CarregarStreamTorrentAsync(torrent[0]);
                return;
            }

            if (Utils.TryGetDataObject(dataObject, DataFormats.Text, out string magnet)
                && magnet.StartsWith("magnet:?", StringComparison.OrdinalIgnoreCase)) // Magnet link
            {
                e.Handled = true;
                _ = CarregarStreamTorrentAsync(magnet);
                return;
            }
        }
    }

    /// <summary>Dispara quando há movimento do mouse sobre os controles (modo cinema).</summary>
    public event EventHandler? MouseDetected;
}
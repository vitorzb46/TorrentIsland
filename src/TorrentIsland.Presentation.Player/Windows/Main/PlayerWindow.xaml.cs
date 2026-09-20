using LibVLCSharp.Shared;
using LibVLCSharp.WPF;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using TorrentIsland.Application.Interfaces;
using TorrentIsland.Application.Settings;
using TorrentIsland.Infrastructure.Interfaces;
using TorrentIsland.Infrastructure.Services;
using TorrentIsland.Infrastructure.VLC;
using TorrentIsland.Presentation.Player.Common;
using TorrentIsland.Presentation.Player.ViewModel;
using TorrentIsland.Presentation.Player.Windows.Controls;

namespace TorrentIsland.Presentation.Player.Windows.Main;

public partial class PlayerWindow : Wpf.Ui.Controls.FluentWindow
{
    #region Fields + Constructor
    private readonly PlayerViewModel _viewModel;
    private readonly ControlsWindow _controls;
    private readonly DispatcherTimer _inactivityTimer;
    private readonly IDLService _ytDlService;
    private readonly DispatcherTimer _osdTimer;

    public PlayerWindow(IStreamService streamService,
                        IManagers managers,
                        IDLService ytDlService,
                        ITorrentStatusEvent torrentStatus,
                        IFormattingHelper fb,
                        ITorrentService torrentService)
    {
        //RenderOptions.ProcessRenderMode = System.Windows.Interop.RenderMode.SoftwareOnly;

        Managers = managers;

        InitializeComponent();

        var vlc = new VlcPlayerService();

        _viewModel = new PlayerViewModel(vlc.LibVLC, vlc.MediaPlayer, fb);

        DataContext = _viewModel;

        VideoView.MediaPlayer = vlc.MediaPlayer;

        // Janela de controles separada (evita o airspace do HWND nativo bloquear os cliques).
        // Owner é atribuído no Loaded (a janela dona precisa estar visível antes).
        _controls = new ControlsWindow(_viewModel, this, torrentStatus, torrentService);
        _controls.FullscreenRequested += (_, _) => ToggleFullscreen();
        _controls.ActivityDetected += (_, _) => ReiniciarTimerInatividade();
        _controls.Closed += (_, _) => Dispatcher.BeginInvoke(new Action(Close),
                                                             DispatcherPriority.ContextIdle);

        _inactivityTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(5)
        };
        _inactivityTimer.Tick += (_, _) => HideControls();

        _osdTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(2)
        };
        _osdTimer.Tick += (s, e) =>
        {
            OsdNotification.Visibility = Visibility.Collapsed;
            _osdTimer.Stop();
        };

        Loaded += (_, _) =>
        {
            Log.Salvar("Loaded disparado — posicionando controles");
            // Owner garante que a janela de controles fique sempre à frente do player.
            _controls.Owner = this;

            // --- RESOLUÇÃO DO BUG DO MENU FLUTUANTE (Foco do Windows) ---
            this.Activated += (s, e) => _controls.Topmost = true;
            this.Deactivated += (s, e) => _controls.Topmost = false;

            PosicionarControles();
            _controls.ShowActivated = false;
            _controls.Show();
        };
        Closed += (_, _) =>
        {
            Log.Salvar($"Window fechada — dispose do {typeof(PlayerWindow).Name}");
            _controls.FullscreenRequested -= (_, _) => ToggleFullscreen();
            _controls.ActivityDetected -= (_, _) => ReiniciarTimerInatividade();

            LocationChanged -= (_, _) => PosicionarControles();
            SizeChanged -= (_, _) => PosicionarControles();
            StateChanged -= (_, _) => PosicionarControles();

            _inactivityTimer?.Stop();
            _osdTimer?.Stop();

            if (_controls != null)
            {
                _controls.Close();
            }

            //Task.Run(async () =>
            //{
            //    await Managers.SaveEngine().ConfigureAwait(false);
            //});

            _viewModel?.Dispose();
            vlc?.Dispose();

            Environment.Exit(0);
        };

        // Sincroniza a janela de controles com a janela de vídeo.
        LocationChanged += (_, _) => PosicionarControles();
        SizeChanged += (_, _) => PosicionarControles();
        StateChanged += (_, _) => PosicionarControles();

        StreamService = streamService;
        _ytDlService = ytDlService;
    }
    #endregion

    #region Properties
    public string MediaUrl { get; set; } = "";
    private IStreamService StreamService { get; }
    private IManagers Managers { get; }
    #endregion

    #region Public Methods
    public void ReiniciarTimerInatividade()
    {
        if (_viewModel.IsFullscreen)
        {
            _inactivityTimer.Stop();
            _inactivityTimer.Start();
        }
    }
    public async Task CarregarMidiaAsync(string caminhoOuUrl)
    {
        try
        {
            await MediaTimestamp.Load();
            _viewModel.IsVideoVisible = false;
            _viewModel.IsLoading = true;

            var media = await EscolherMidiaAsync(caminhoOuUrl);

            if (media == null) return;

            if (_viewModel.GetMedia() != null)
            {
                await MediaTimestamp.SaveCache(PlayerViewModel.OldFilePath, _viewModel.MediaTime);
            }

            KeyGenerator.Hash(PlayerViewModel.FilePath);
            var timeCached = await MediaTimestamp.LoadCache(PlayerViewModel.FilePath);
            _viewModel.SetMedia(media, timeCached.Time);
            await Utils.AtualizarUIAsync(async () =>
            {
                await _viewModel.PopulateTracksAsync();
                _viewModel.SetPause(false);
                ShowControls();
                _viewModel.IsLoading = false;
                VideoView.InvalidateVisual();
                Utils.VideoView_Background_Black();
                await Task.Delay(300); //Tempo de espera para evitar artefato visual
                _viewModel.IsVideoVisible = true;
                _viewModel.IsPlaying = true;
            });
        }
        catch (Exception ex)
        {
            Log.Salvar($"Erro ao carregar mídia: {ex.Message} {ex.StackTrace} {ex.InnerException}");
            MessageBox.Show($"Não foi possível carregar a mídia: {caminhoOuUrl}", "Player",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            _viewModel.IsLoading = false;
        }
    }

    public async Task CarregarStreamTorrentAsync(string caminhoOuUrl)
    {
        try
        {
            _viewModel.IsVideoVisible = false;
            _viewModel.LoadingMessage = "Iniciando streaming...";
            _viewModel.IsLoading = true;

            string streamUrl = await Task.Run(async () =>
            {
                return await StreamService.ToPlayerAsync(caminhoOuUrl);
            });
            AppSettings.OneStream = true;
            await CarregarMidiaAsync(streamUrl);
        }
        catch (Exception ex)
        {
            Log.Salvar($"Erro ao iniciar stream de torrent: {ex.Message}");
            MessageBox.Show($"Não foi possível iniciar o stream do torrent!", "Player",
                MessageBoxButton.OK, MessageBoxImage.Error);

            if (_viewModel.GetMediaPlayer != null)
                _viewModel.IsVideoVisible = true;
        }
        finally
        {
            _viewModel.IsLoading = false;
        }
    }
    #endregion

    #region Private Methods
    private async Task<Media?> EscolherMidiaAsync(string caminhoOuUrl)
    {
        Media? media;
        if (File.Exists(caminhoOuUrl))
        {
            media = new Media(_viewModel.LibVLC, caminhoOuUrl, FromType.FromPath);
            PlayerViewModel.FilePath = caminhoOuUrl;
        }
        else if (Uri.TryCreate(caminhoOuUrl, UriKind.Absolute, out _))
        {
            media = new Media(_viewModel.LibVLC, caminhoOuUrl, FromType.FromLocation);

            var torrents = await Managers.ObterTorrentsAsync();
            if (torrents.Count > 0)
            {
                PlayerViewModel.FilePath = torrents.Select(v => v.Value.FullPath).FirstOrDefault()!;
            }
        }
        else
        {
            var streamUrl = await _ytDlService.GetStreamingUrl(caminhoOuUrl);
            Log.Salvar($"Iniciando stream yt-dlp: {streamUrl}");
            media = new Media(_viewModel.LibVLC, streamUrl, FromType.FromLocation);
            if (media == null)
            {
                MessageBox.Show($"Url: {caminhoOuUrl} não suportada.", "Player", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
        }

        return media;
    }
    /// <summary>
    /// Timer da sobreposição de tempo da legenda (On-Screen Display).
    /// </summary>
    private void ReiniciarTimerOsd()
    {
        _osdTimer.Stop();
        _osdTimer.Start();
    }
    /// <summary>Posiciona a janela de controles na parte inferior da janela de vídeo.</summary>
    private void PosicionarControles()
    {
        Dispatcher.BeginInvoke(new Action(() =>
        {
            VideoView.Width = 0;
            VideoView.Height = 0;
            VideoView.Width = double.NaN;
            VideoView.Height = double.NaN;
            VideoView.InvalidateVisual();
        }), DispatcherPriority.Render);

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
        if (_viewModel.IsFullscreen)
        {
            _controls.Visibility = Visibility.Collapsed;
            Mouse.OverrideCursor = Cursors.None;
        }
        ReiniciarTimerInatividade();
    }

    private void ToggleFullscreen()
    {
        if (WindowState == WindowState.Normal)
        {
            // Entra em tela cheia
            Background = Brushes.Black;
            WindowStyle = WindowStyle.None;
            WindowState = WindowState.Maximized;
            _viewModel.IsFullscreen = true;

            MyTitleBar?.Visibility = Visibility.Collapsed;
        }
        else
        {
            // Volta para o modo janela
            Background = Brushes.Black;
            WindowStyle = WindowStyle.SingleBorderWindow;
            WindowState = WindowState.Normal;
            _viewModel.IsFullscreen = false;

            MyTitleBar?.Visibility = Visibility.Visible;
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
                _viewModel.LoadingMessage = "Carregando mídia...";
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
    #endregion

    #region Shortcuts
    protected override void OnPreviewMouseDoubleClick(MouseButtonEventArgs e)
    {
        base.OnPreviewMouseDoubleClick(e);

        if (e.ChangedButton == MouseButton.Left) ToggleFullscreen();
    }

    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        //Log.Salvar($"Tecla: {e.Key} | SystemKey: {e.SystemKey} | Modifiers: {Keyboard.Modifiers}");
        base.OnPreviewKeyDown(e);

        if (e.Key == Key.Escape && _viewModel.IsFullscreen) ToggleFullscreen();

        if (e.Key == Key.F11) ToggleFullscreen();

        if (e.Key == Key.Space) _viewModel.TogglePlay();

        if (e.Key == Key.Left) _viewModel.RetrocederTempo();

        if (e.Key == Key.Right) _viewModel.AvancarTempo();

        if (e.Key == Key.Down) _viewModel.ToggleVolume(false); ShowControls(); e.Handled = true;

        if (e.Key == Key.Up) _viewModel.ToggleVolume(true); ShowControls(); e.Handled = true;

        if (e.Key == Key.M) _viewModel.ToggleMute();

        if (e.Key == Key.OemOpenBrackets || e.Key == Key.Oem5)
        {
            OsdNotification.Visibility = Visibility.Visible;
            _viewModel.ToggleDelaySpu(false);
            ReiniciarTimerOsd();
            e.Handled = true;
        }
        else if (e.Key == Key.OemCloseBrackets || e.Key == Key.Oem6)
        {
            OsdNotification.Visibility = Visibility.Visible;
            _viewModel.ToggleDelaySpu(true);
            ReiniciarTimerOsd();
            e.Handled = true;
        }

        if (e.Key == Key.V && Keyboard.Modifiers == ModifierKeys.Control)
        {
            var dataObject = Clipboard.GetDataObject();
            if (dataObject == null) return;

            if (Utils.TryGetDataObject(dataObject, DataFormats.FileDrop, out string[] arquivos)
                && Utils.ExtensoesVideo.Contains(Path.GetExtension(arquivos[0]).ToLowerInvariant())) // Arquivo das extensões de vídeo suportadas
            {
                e.Handled = true;
                _viewModel.LoadingMessage = "Carregando mídia...";
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
                && magnet.StartsWith("magnet:?", StringComparison.OrdinalIgnoreCase) || magnet.StartsWith("http://itorrents.net", StringComparison.OrdinalIgnoreCase)) // Magnet link ou Link direto
            {
                e.Handled = true;
                _ = CarregarStreamTorrentAsync(magnet);
                return;
            }

            if (Utils.TryGetDataObject(dataObject, DataFormats.Text, out string ytDlp))
            {
                e.Handled = true;
                _viewModel.LoadingMessage = "Carregando mídia...";
                _ = CarregarMidiaAsync(ytDlp);
                return;
            }
        }
    }
    #endregion

    /// <summary>Dispara quando há movimento do mouse sobre os controles (modo cinema).</summary>
    public event EventHandler? MouseDetected;
}
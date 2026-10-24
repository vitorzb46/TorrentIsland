using LibVLCSharp.WPF;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using TorrentIsland.Application.Interfaces;
using TorrentIsland.Application.Medias.Enums;
using TorrentIsland.Application.Medias.Events;
using TorrentIsland.Infrastructure.Interfaces;
using TorrentIsland.Infrastructure.Logging;
using TorrentIsland.Infrastructure.VLC;
using TorrentIsland.Presentation.Player.Abstractions;
using TorrentIsland.Presentation.Player.Common;
using TorrentIsland.Presentation.Player.ContextMenus;
using TorrentIsland.Presentation.Player.ViewModel;
using TorrentIsland.Presentation.Player.Windows.Controls;
using TorrentIsland.Presentation.Player.Windows.Settings;
using TorrentIsland.Presentation.Player.Windows.StreamUrl;
using TorrentIsland.Presentation.Player.Windows.WebSearch;
using Wpf.Ui.Controls;
using Brushes = System.Windows.Media.Brushes;
using Clipboard = System.Windows.Clipboard;
using Cursor = System.Windows.Input.Cursor;
using Cursors = System.Windows.Input.Cursors;
using DataFormats = System.Windows.DataFormats;
using DragEventArgs = System.Windows.DragEventArgs;
using IDataObject = System.Windows.IDataObject;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;

namespace TorrentIsland.Presentation.Player.Windows.Main;

public partial class PlayerWindow : FluentWindow, IPlayerUiContext
{
    #region Fields + Constructor
    private readonly PlayerViewModel _viewModel;
    private readonly ControlsWindow _controls;
    private readonly PlayerSessionTimers _timers;
    private readonly MediaDropHandler _dropHandler;
    private readonly IShortcutManager _scManager;
    private readonly ILocalMediaLoader _localMedia;
    private readonly IStreamTorrentLoader _streamLoader;
    private readonly ControlsWindowPositioner _controlsPositioner;
    private readonly IMediasFolderLoader _folderLoader;
    private readonly TorrentSearchWindow _torrentSearch;
    private Preferences? _preferencesWindow;

    public PlayerWindow(IManagers managers,
                        ITorrentStatusEvent torrentStatus,
                        ITorrentService torrentService,
                        IShortcutManager shortcutManager,
                        VlcPlayerService vlc,
                        PlayerViewModel viewModel,
                        PlayerSessionTimers timers,
                        MediaDropHandler dropHandler,
                        Func<IPlayerUiContext, ILocalMediaLoader> localLoaderFactory,
                        Func<IPlayerUiContext, IMediasFolderLoader> folderLoaderFactory,
                        Func<IPlayerUiContext, IStreamTorrentLoader> torrentLoaderFactory,
                        Func<IPlayerUiContext, IControlsWindow, ControlsWindowPositioner> positionerFactory)
    {
        InitializeComponent();
        Managers = managers;
        TitleLeft.Text = "TorrentIsland";

        _viewModel = viewModel;
        _timers = timers;
        _dropHandler = dropHandler;
        _scManager = shortcutManager;
        _scManager.RegisterPlayerShortcuts(this, _viewModel);
        
        _controls = new ControlsWindow(_viewModel, this, torrentStatus, torrentService);
        _torrentSearch = new TorrentSearchWindow(_viewModel, this, torrentService, torrentStatus);
        
        _localMedia = localLoaderFactory(this);
        _folderLoader = folderLoaderFactory(this);
        _streamLoader = torrentLoaderFactory(this);
        _controlsPositioner = positionerFactory(this, _controls);

        DataContext = _viewModel;
        VideoView.MediaPlayer = vlc.MediaPlayer;
        
        SubscribeEvents();

        Loaded += (_, _) =>
        {
            // Owner garante que a janela de controles fique sempre à frente do player.
            _controls.Owner = this;

            // --- RESOLUÇÃO DO BUG DO MENU FLUTUANTE (Foco do Windows) ---
            Activated += (s, e) => ReforcarTopmostControles();
            Deactivated += (s, e) => _controls.Topmost = false;

            PosicionarControles();
            _controls.ShowActivated = false;
            _controls.Show();

            _timers.AutoSaverStart();
        };
        Closed += (_, _) =>
        {
            UnsubscribeEvents();

            _timers.Dispose();

            _controls?.Close();

            _viewModel?.Dispose();
            vlc?.Dispose();
            Log.Salvar("[Shutdown] PlayerWindow fechada.");
            Environment.Exit(0);
        };
    }
    #endregion

    #region Properties
    public string MediaUrl { get; set; } = "";
    private IManagers Managers { get; }

    public PlayerViewModel ViewModel => _viewModel;
    public VideoView PwVideoView { get => VideoView; set => VideoView = value; }
    public Window Property => this;
    #endregion

    #region Public Methods
    public void ShowPreferencesWindow()
    {
        if (_preferencesWindow is { IsLoaded: true })
        {
            _preferencesWindow.Activate();
            return;
        }
        _preferencesWindow ??= new Preferences { Owner = _controls };
        _preferencesWindow.Closed += (_, _) => _preferencesWindow = null;
        _preferencesWindow.ReloadData();
        _preferencesWindow.Show();
        _preferencesWindow.Activate();
    }

    public async Task<bool> ProcessPaste(KeyEventArgs e)
    {
        var mediaLoader = new MediaLoader(this);

        IDataObject? obj = Clipboard.GetDataObject();
        if (obj == null) return false;

        var files = (string[])obj.GetData(DataFormats.FileDrop);
        var text = (string)obj.GetData(DataFormats.Text);

        var resultado = await mediaLoader.InputAsync(files, text);

        if (resultado) e.Handled = true;
        return resultado;
    }

    public async Task<bool> OpenUrl(KeyEventArgs e, string? title)
    {
        OpenUrlStream(title);
        e.Handled = true;
        return true;
    }
    public void ShowControls() => SCShowControls();
    public void ApplyBlackBackground() => Utils.VideoView_Background_Black();
    public void ReiniciarTimerInatividade() => _timers.ReiniciarTimerInatividade();
    public void AbrirMenu() => _timers.AbrirMenu();
    public void FecharMenu() => _timers.FecharMenu();

    public async Task CarregarPastaAsync(List<string>? folder) =>
        await _folderLoader.LoadAsync(folder).ConfigureAwait(false);

    public async Task CarregarMidiaAsync(string caminhoOuUrl) =>
        await _localMedia.LoadAsync(caminhoOuUrl).ConfigureAwait(false);

    public async Task CarregarStreamTorrentAsync(string caminhoOuUrl) =>
        await _streamLoader.LoadAsync(caminhoOuUrl).ConfigureAwait(false);
    #endregion

    #region Private Methods
    /// <summary>Posiciona a janela de controles na parte inferior da janela de vídeo.</summary>
    private void PosicionarControles() => _controlsPositioner.RefreshLayout();

    // --- Modo cinema ---
    private void SCShowControls()
    {
        if (_controls.Visibility != Visibility.Visible)
        {
            UpdateControlsDisplay(Visibility.Visible, Cursors.Arrow);
            _controlsPositioner.RefreshLayout();
        }
        _timers.ReiniciarTimerInatividade();
    }

    private void HideControls()
    {
        if (!_viewModel.IsFullscreen) return;
        UpdateControlsDisplay(Visibility.Collapsed, Cursors.None);
    }

    private void UpdateControlsDisplay(Visibility visibility, Cursor cursors)
    {
        _controls.Visibility = visibility;
        Mouse.OverrideCursor = cursors;
    }

    public void ToggleFullscreen()
    {
        if (WindowState == WindowState.Normal)
        {
            // Entra em tela cheia
            UpdateWindowDisplayMode(Visibility.Collapsed, WindowStyle.None, WindowState.Maximized, true);
        }
        else
        {
            // Volta para o modo janela
            UpdateWindowDisplayMode(Visibility.Visible, WindowStyle.SingleBorderWindow, WindowState.Normal, false);
        }

        _viewModel.IsFullscreen = WindowState == WindowState.Maximized;

        // Executa o posicionamento um milissegundo depois, garantindo que o Windows já mudou de tamanho
        Dispatcher.BeginInvoke(new Action(() =>
        {
            ShowControls();
        }), DispatcherPriority.Render);
    }

    private void UpdateWindowDisplayMode(Visibility visibility, WindowStyle style, WindowState state, bool isFullscreen)
    {
        MyTitleBar?.Visibility = visibility;
        Background = Brushes.Black;
        WindowStyle = style;
        WindowState = state;
        _viewModel.IsFullscreen = isFullscreen;
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

    private void PlayerWindow_DragEnter(object sender, DragEventArgs e) => ProcessDragInteraction(sender, e);

    private void PlayerWindow_DragOver(object sender, DragEventArgs e) => ProcessDragInteraction(sender, e);

    private void ProcessDragInteraction(object sender, DragEventArgs e)
    {
        _dropHandler.OnDrop(sender, e);
        _dropHandler.HandleDragDrop(sender, e);
    }

    private async void PlayerWindow_Drop(object sender, DragEventArgs e)
    {
        _dropHandler.OnDrop(sender, e);
        await _dropHandler.LoadDroppedMediaAsync(this).ConfigureAwait(false);
    }

    private void ThumbPlayPause_Click(object? sender, EventArgs e) => _viewModel.TogglePlay();

    private async void ThumbStop_Click(object? sender, EventArgs e)
    {
        if (_viewModel.IsPlaying)
        {
            _viewModel.IsPlaying = false;
            BtnPlayPause.ImageSource = (ImageSource)FindResource("IconPlay");
            BtnPlayPause.Description = "Reproduzir";
        }
        _viewModel.StopPlaybackState();
        _viewModel.FilePath = string.Empty;
        await _viewModel.PlayNextMedia();
    }

    private void ThumbFullscreen_Click(object? sender, EventArgs e)
    {
        ToggleFullscreen();
        AtivarJanela();
    }

    private void AtivarJanela()
    {
        if (WindowState == WindowState.Minimized)
            WindowState = WindowState.Normal;

        Activate();
        Topmost = true;
        Topmost = false;
        Focus();
    }

    private void ReforcarTopmostControles()
    {
        if (_controls is null) return;
        Keyboard.Focus(this);
        _controls.Topmost = false;
        _controls.Topmost = true;
    }

    private void OverlayGrid_ContextMenuOpening(object sender, ContextMenuEventArgs e)
    {
        e.Handled = true;
        AbrirMenu();
        _timers.ReiniciarTimerInatividade();
        ReforcarTopmostControles();

        var builder = new PlayerContextMenuBuilder(_viewModel);
        var menu = builder.Build();
        menu.PlacementTarget = OverlayGrid;
        menu.Placement = System.Windows.Controls.Primitives.PlacementMode.MousePoint;
        menu.Closed += (_, _) =>
        {
            FecharMenu();
            _timers.ReiniciarTimerInatividade();
            ReforcarTopmostControles();
        };
        menu.IsOpen = true;
    }

    private void OverlayGrid_MouseDown(object sender, MouseButtonEventArgs e) =>
        ReforcarTopmostControles();

    private void ShowOsd(string texto)
    {
        _viewModel.OsdMessage = texto;
        OsdNotification.Visibility = Visibility.Visible;
        _timers.ReiniciarTimerOsd();
    }

    private void OverlayGrid_MouseMove(object sender, MouseEventArgs e)
    {
        if (!_viewModel.IsFullscreen) return;
        Mouse.OverrideCursor = Cursors.Arrow;
    }

    private void SubscribeEvents()
    {
        OverlayGrid.ContextMenuOpening += OverlayGrid_ContextMenuOpening;

        PlayerViewModel.FilePathChanged += OnFilePathChanged;

        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        _viewModel.OpenFileExternalRequested += OnOpenFileExternalRequested;
        _viewModel.PlaybackActionRequested += OnPlaybackActionRequested;
        _viewModel.TorrentSearchRequested += OnTorrentSearchRequested;
        _viewModel.TorrentQueueRequested += OnTorrentQueueRequested;
        _viewModel.OsdMessageChanged += OnOsdMessageChanged;
        _viewModel.OpenUrlStreamRequested += OnOpenUrlStreamRequested;

        // Sincroniza a janela de controles com a janela de vídeo.
        LocationChanged += (_, _) => PosicionarControles();
        SizeChanged += (_, _) => PosicionarControles();
        StateChanged += (_, _) => PosicionarControles();

        // Janela de controles separada (evita o airspace do HWND nativo bloquear os cliques).
        // Owner é atribuído no Loaded (a janela dona precisa estar visível antes).        
        _controls.FullscreenRequested += (_, _) => ToggleFullscreen();
        _controls.ActivityDetected += (_, _) => _timers.ReiniciarTimerInatividade();
        _controls.Closed += (_, _) =>
        {
            _timers.InactivityStop();
            Dispatcher.BeginInvoke(new Action(Close), DispatcherPriority.ContextIdle);
        };

        _timers.InactivityTimer.Tick += (_, _) =>
        {
            if (!_viewModel.IsFullscreen || _timers.MenuAberto) return;
            HideControls();
        };

        _timers.OsdTimer.Tick += (_, _) =>
        {
            OsdNotification.Visibility = Visibility.Collapsed;
            _timers.OsdStop();
        };
    }

    private void UnsubscribeEvents()
    {
        _controls.FullscreenRequested -= (_, _) => ToggleFullscreen();
        _controls.ActivityDetected -= (_, _) => _timers.ReiniciarTimerInatividade();
        OverlayGrid.ContextMenuOpening -= OverlayGrid_ContextMenuOpening;
        PlayerViewModel.FilePathChanged -= OnFilePathChanged;
        _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        _viewModel.OpenFileExternalRequested -= OnOpenFileExternalRequested;
        _viewModel.PlaybackActionRequested -= OnPlaybackActionRequested;
        _viewModel.TorrentSearchRequested -= OnTorrentSearchRequested;
        _viewModel.TorrentQueueRequested -= OnTorrentQueueRequested;
        _viewModel.OsdMessageChanged -= OnOsdMessageChanged;
        _viewModel.OpenUrlStreamRequested -= OnOpenUrlStreamRequested;

        LocationChanged -= (_, _) => PosicionarControles();
        SizeChanged -= (_, _) => PosicionarControles();
        StateChanged -= (_, _) => PosicionarControles();
    }
    #endregion

    #region Shortcuts
    protected override void OnPreviewMouseDoubleClick(MouseButtonEventArgs e)
    {
        base.OnPreviewMouseDoubleClick(e);

        if (e.ChangedButton == MouseButton.Left) ToggleFullscreen();
    }

    protected override async void OnPreviewKeyDown(KeyEventArgs e)
    {
        // Log.Salvar($"Tecla: {e.Key} | SystemKey: {e.SystemKey} | Modifiers: {Keyboard.Modifiers}");

        bool shortcut = await _scManager.TryHandleAsync(e);

        if (shortcut) e.Handled = true;
    }
    #endregion

    /// <summary>Dispara quando há movimento do mouse sobre os controles (modo cinema).</summary>
    public event EventHandler? MouseDetected;

    private void OnFilePathChanged()
    {
        Dispatcher.Invoke(() =>
        {
            TitleCenter.Text = _viewModel.FileName;
            Title = string.IsNullOrEmpty(_viewModel.FileName)
                ? "TorrentIsland"
                : $"{_viewModel.FileName} - TorrentIsland";
        });
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(PlayerViewModel.IsPlaying)) return;

        Dispatcher.Invoke(() =>
        {
            BtnPlayPause.ImageSource = (ImageSource)FindResource(
                _viewModel.IsPlaying ? "IconPause" : "IconPlay");
            BtnPlayPause.Description = _viewModel.IsPlaying ? "Pausar" : "Reproduzir";
        });
    }

    private async void OnOpenFileExternalRequested(object? sender, ExternalMediaEventArgs e)
    {
        try
        {
            switch (e.Type)
            {
                case FileType.Audio: await _controls.ProcessarAudioExternoAsync(); return;
                case FileType.Subtitle: await _controls.ProcessarLegendaExternaAsync(); return;
                case FileType.Video: await _controls.ProcessarMidiaAsync(); return;
                case FileType.Torrent: await _controls.ProcessarTorrentAsync(); return;
                case FileType.Folder: await _controls.AbrirPastaAsync(); return;
                default: Log.Salvar($"[OpenFile] FileType não tratado: {e.Type}"); return;
            }
        }
        catch (Exception ex)
        {
            Log.Salvar($"Erro ao processar arquivo externo: {ex.Message} {ex.StackTrace} {ex.InnerException}");
        }
    }

    private void OnPlaybackActionRequested(object? sender, PlaybackEventArgs e)
    {
        try
        {
            switch (e.Type)
            {
                case PlaybackType.Stop: ThumbStop_Click(sender, e); return;
                case PlaybackType.Fullscreen: ThumbFullscreen_Click(sender, e); return;
                default: Log.Salvar($"[Playback] PlaybackType não tratado: {e.Type}"); return;
            }
        }
        catch (Exception ex)
        {
            Log.Salvar($"Erro ao processar ação de reprodução: {ex.Message} {ex.StackTrace} {ex.InnerException}");
        }
    }

    private void OnTorrentSearchRequested(object? sender, EventArgs e) =>
        _controls.BuscarTorrentMenuItem_Click(sender, e);

    private async void OnTorrentQueueRequested(object? sender, EventArgs e)
    {
        _controls.BuscarTorrentMenuItem_Click(sender, e);
        _torrentSearch.BtnAlternarTela_Click(sender, e);
    }
    private void OnOsdMessageChanged(object? sender, EventArgs e)
    {
        OsdNotification.Visibility = Visibility.Visible;
        _timers.ReiniciarTimerOsd();
    }

    private void OnOpenUrlStreamRequested(object? sender, EventArgs e)
    {
        if (sender is null) return;
        if ((string)sender == "Youtube")
        {
            OpenUrlStream("Youtube");
        }
        else
        {
            OpenUrlStream("Mídia Streaming");
        }
    }

    private void OpenUrlStream(string? title = null)
    {
        var window = new OpenURL(this, title)
        {
            Owner = GetWindow(this)
        };
        window?.Show();
    }

    protected override void OnClosed(EventArgs e)
    {
        _timers.AutoSaverStop();

        Task.Run(async () =>
        {
            await _timers.SaveEngineOnCloseAsync(Managers, "Shutdown");
        });

        base.OnClosed(e);
    }
}
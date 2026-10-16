using LibVLCSharp.Shared;
using LibVLCSharp.WPF;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using TorrentIsland.Application.Interfaces;
using TorrentIsland.Application.Medias.Enums;
using TorrentIsland.Application.Medias.Events;
using TorrentIsland.Application.Settings;
using TorrentIsland.Infrastructure.Interfaces;
using TorrentIsland.Infrastructure.Logging;
using TorrentIsland.Infrastructure.Services;
using TorrentIsland.Infrastructure.VLC;
using TorrentIsland.Presentation.Player.Common;
using TorrentIsland.Presentation.Player.ContextMenus;
using TorrentIsland.Presentation.Player.ViewModel;
using TorrentIsland.Presentation.Player.Windows.Controls;
using TorrentIsland.Presentation.Player.Windows.StreamUrl;
using TorrentIsland.Presentation.Player.Windows.WebSearch;
using Brushes = System.Windows.Media.Brushes;
using Clipboard = System.Windows.Clipboard;
using Cursors = System.Windows.Input.Cursors;
using DataFormats = System.Windows.DataFormats;
using DragDropEffects = System.Windows.DragDropEffects;
using DragEventArgs = System.Windows.DragEventArgs;
using IDataObject = System.Windows.IDataObject;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using MessageBox = System.Windows.MessageBox;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;

namespace TorrentIsland.Presentation.Player.Windows.Main;

public partial class PlayerWindow : Wpf.Ui.Controls.FluentWindow
{
    #region Fields + Constructor
    private readonly PlayerViewModel _viewModel;
    private readonly ControlsWindow _controls;
    private readonly IDLService _ytDlService;
    private readonly PlayerSessionTimers _timers;
    private readonly TorrentSearchWindow _torrentSearch;
    private readonly MediaFactory _mediaFactory;

    public PlayerWindow(IStreamService streamService,
                        IManagers managers,
                        IDLService ytDlService,
                        ITorrentStatusEvent torrentStatus,
                        IFormattingHelper fb,
                        ITorrentService torrentService)
    {
        InitializeComponent();

        TitleLeft.Text = "TorrentIsland";

        var vlc = new VlcPlayerService();

        _viewModel = new PlayerViewModel(vlc.LibVLC, vlc.MediaPlayer, fb);

        DataContext = _viewModel;

        VideoView.MediaPlayer = vlc.MediaPlayer;

        _controls = new ControlsWindow(_viewModel, this, torrentStatus, torrentService);

        Managers = managers;
        StreamService = streamService;
        _ytDlService = ytDlService;
        _timers = new PlayerSessionTimers(_viewModel, Managers);
        _torrentSearch = new TorrentSearchWindow(_viewModel, this, torrentService, torrentStatus);
        _mediaFactory = new MediaFactory(vlc.LibVLC, _ytDlService, Managers);
        
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
        };
        Closed += (_, _) =>
        {
            UnsubscribeEvents();

            _timers.InactivityStop();
            _timers.OsdStop();
            
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
    private IStreamService StreamService { get; }
    private IManagers Managers { get; }
    #endregion

    #region Public Methods    
    public void ReiniciarTimerInatividade() => _timers.ReiniciarTimerInatividade();
    public void AbrirMenu() => _timers.AbrirMenu();
    public void FecharMenu() => _timers.FecharMenu();
    
    public async Task CarregarPastaAsync(List<string>? folder)
    {
        // TODO
        foreach (var file in folder!)
        {
            if (!await TryAddMediaAsync(file))
                continue;
        }
        await _viewModel.PlayNextMedia();
    }

    public async Task CarregarMidiaAsync(string caminhoOuUrl)
    {
        try
        {
            await _viewModel.LoadingStateAsync("Carregando mídia...", async () =>
            {
                await PrepareForNewMediaAsync();
                await MediaTimestamp.Load();
                var factory = await _mediaFactory.CreateAsync(caminhoOuUrl);
                if (factory?.Media is null) return;

                var media = factory.Media;
                PlayerViewModel.FilePath = factory.FilePath;
                var timeCached = await MediaTimestamp.LoadCache(factory.FilePath);
                _viewModel.SetMedia(media, timeCached.Time);

                await Utils.AtualizarUIAsync(async () =>
                {
                    await _viewModel.PopulateTracksAsync();
                    _viewModel.SetPause(false);
                    ShowControls();
                    VideoView.InvalidateVisual();
                    Utils.VideoView_Background_Black();
                    await Task.Delay(300); //Tempo de espera para evitar artefato visual
                });
            });
        }
        catch (Exception ex)
        {
            Log.Salvar($"Erro ao carregar mídia: {ex.Message} {ex.StackTrace} {ex.InnerException}");
            MessageBox.Show($"Não foi possível carregar a mídia: {caminhoOuUrl}", "Player",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    public async Task CarregarStreamTorrentAsync(string caminhoOuUrl)
    {
        try
        {
            await _viewModel.LoadingStateAsync("Iniciando streaming...", async () =>
            {
                string streamUrl = await Task.Run(async () =>
                {
                    return await StreamService.ToPlayerAsync(caminhoOuUrl);
                });
                AppSettings.OneStream = true;
                await CarregarMidiaAsync(streamUrl);
            });
        }
        catch (Exception ex)
        {
            Log.Salvar($"Erro ao iniciar stream de torrent: {ex.Message}");
            MessageBox.Show($"Não foi possível iniciar o stream do torrent!", "Player",
                MessageBoxButton.OK, MessageBoxImage.Error);

            if (_viewModel.GetMediaPlayer != null)
                _viewModel.IsVideoVisible = true;
        }
    }
    #endregion

    #region Private Methods
    private async Task PrepareForNewMediaAsync()
    {
        var media = _viewModel.GetMedia();
        if (media is null) return;
        await _viewModel.SaveCacheAsync();
        _viewModel.MediaDispose();
    }

    private async Task<bool> TryAddMediaAsync(string file)
    {
        var factory = await _mediaFactory.CreateAsync(file);
        if (factory?.Media == null) return false;
        _viewModel.AddMedia(factory.Media);
        return true;
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
        else if (WindowState == WindowState.Maximized)
        {
            _controls.Width = SystemParameters.WorkArea.Width;
            _controls.Left = SystemParameters.WorkArea.Left;
            _controls.Top = SystemParameters.WorkArea.Bottom - _controls.Height;
        }
        // Se estiver em modo janela normal, usamos a matemática baseada no Player (this)
        else
        {
            _controls.Width = ActualWidth;
            _controls.Left = Left;
            _controls.Top = Top + ActualHeight - _controls.Height;
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
        _timers.ReiniciarTimerInatividade();
    }

    private void HideControls()
    {
        if (!_viewModel.IsFullscreen) return;

        _controls.Visibility = Visibility.Collapsed;
        Mouse.OverrideCursor = Cursors.None;
    }

    private void ToggleFullscreen()
    {
        if (WindowState == WindowState.Normal)
        {
            // Entra em tela cheia
            MyTitleBar?.Visibility = Visibility.Collapsed;
            Background = Brushes.Black;
            WindowStyle = WindowStyle.None;
            WindowState = WindowState.Maximized;
            _viewModel.IsFullscreen = true;
        }
        else
        {
            // Volta para o modo janela
            MyTitleBar?.Visibility = Visibility.Visible;
            Background = Brushes.Black;
            WindowStyle = WindowStyle.SingleBorderWindow;
            WindowState = WindowState.Normal;
            _viewModel.IsFullscreen = false;
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

    private void ThumbPlayPause_Click(object? sender, EventArgs e) => _viewModel.TogglePlay();

    private void ThumbStop_Click(object? sender, EventArgs e)
    {
        if (_viewModel.IsPlaying)
        {
            _viewModel.IsPlaying = false;
            BtnPlayPause.ImageSource = (ImageSource)FindResource("IconPlay");
            BtnPlayPause.Description = "Reproduzir";
        }
        _viewModel.StopPlaybackState();
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

        _timers.InactivityTick(() =>
        {
            if (!_viewModel.IsFullscreen || _timers.MenuAberto) return;
            HideControls();
        });

        _timers.OsdTick(() =>
        {
            OsdNotification.Visibility = Visibility.Collapsed;
            _timers.OsdStop();
        });
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
        base.OnPreviewKeyDown(e);

        bool ExecuteAudioDelay(bool advance, double value)
        { _viewModel.DelayAudioCommand.Execute((advance, value)); return true; }

        bool ExecuteSubDelay(bool advance, double value)
        { _viewModel.DelaySpuCommand.Execute((advance, value)); return true; }

        bool Adjust(Action action, Func<string> osdMessage)
        {
            action();
            ShowOsd(osdMessage());
            return true;
        }

        bool ExecuteCommand(ICommand cmd)
        {
            if (cmd.CanExecute(null)) cmd.Execute(null);
            return true;
        }

        var mods = Keyboard.Modifiers;

        switch (e.Key)
        {
            case Key.Escape when _viewModel.IsFullscreen:
            case Key.F11: ToggleFullscreen(); return;
            case Key.Space: _viewModel.TogglePlay(); return;
            case Key.Left: _viewModel.RetrocederTempo(); return;
            case Key.Right: _viewModel.AvancarTempo(); return;
            case Key.M: _viewModel.ToggleMute(); return;
            case Key.Down: _viewModel.ToggleVolume(false); ShowControls(); e.Handled = true; return;
            case Key.Up: _viewModel.ToggleVolume(true); ShowControls(); e.Handled = true; return;
            case Key.J: ExecuteAudioDelay(true, 0.5); return;
            case Key.K: ExecuteAudioDelay(false, 0.5); return;
        }

        var handled = (e.Key, mods) switch
        {
            (Key.T, ModifierKeys.Control | ModifierKeys.Shift) => ExecuteCommand(_viewModel.OpenTorrentFileCommand),
            (Key.S, ModifierKeys.Control | ModifierKeys.Shift) => ExecuteCommand(_viewModel.StopPlayerCommand),

            (Key.T, ModifierKeys.Control) => await OpenUrl(e, "Mídia Streaming"),
            (Key.Y, ModifierKeys.Control) => await OpenUrl(e, "Youtube"),
            (Key.V, ModifierKeys.Control) => await ProcessPaste(e),
            (Key.O, ModifierKeys.Control) => ExecuteCommand(_viewModel.OpenVideoCommand),
            (Key.P, ModifierKeys.Control) => ExecuteCommand(_viewModel.OpenFolderCommand),
            (Key.U, ModifierKeys.Control) => ExecuteCommand(_viewModel.OpenSubtitleExternalCommand),
            (Key.I, ModifierKeys.Control) => ExecuteCommand(_viewModel.OpenAudioExternalCommand),

            (Key.Oem5 or Key.OemOpenBrackets, ModifierKeys.None) => ExecuteSubDelay(true, 0.5),
            (Key.Oem6 or Key.OemCloseBrackets, ModifierKeys.None) => ExecuteSubDelay(false, 0.5),

            (Key.Oem5 or Key.OemOpenBrackets, ModifierKeys.Control) => ExecuteSubDelay(true, 5.0),
            (Key.Oem6 or Key.OemCloseBrackets, ModifierKeys.Control) => ExecuteSubDelay(false, 5.0),

            // Brilho
            (Key.B, ModifierKeys.Control) => Adjust(() =>
                _viewModel.DecrementBrightness(), () => $"Brilho: {_viewModel.Brightness:P0}"),

            (Key.B, ModifierKeys.Shift) => Adjust(() =>
                _viewModel.IncrementBrightness(), () => $"Brilho: {_viewModel.Brightness:P0}"),

            // Contraste
            (Key.C, ModifierKeys.Control) => Adjust(() =>
                _viewModel.DecrementContrast(), () => $"Contraste: {_viewModel.Contrast:P0}"),

            (Key.C, ModifierKeys.Shift) => Adjust(() =>
                _viewModel.IncrementContrast(), () => $"Contraste: {_viewModel.Contrast:P0}"),

            // Matiz
            (Key.H, ModifierKeys.Control) => Adjust(() =>
                _viewModel.DecrementHue(), () => $"Matiz: {_viewModel.Hue}"),

            (Key.H, ModifierKeys.Shift) => Adjust(() =>
                _viewModel.IncrementHue(), () => $"Matiz: {_viewModel.Hue}"),

            // Saturação
            (Key.S, ModifierKeys.Control) => Adjust(() =>
                _viewModel.DecrementSaturation(), () => $"Saturação: {_viewModel.Saturation:P0}"),

            (Key.S, ModifierKeys.Shift) => Adjust(() =>
                _viewModel.IncrementSaturation(), () => $"Saturação: {_viewModel.Saturation:P0}"),

            // Gamma
            (Key.G, ModifierKeys.Control) => Adjust(() =>
                _viewModel.DecrementGamma(), () => $"Gamma: {_viewModel.Gamma:F1}"),

            (Key.G, ModifierKeys.Shift) => Adjust(() =>
                _viewModel.IncrementGamma(), () => $"Gamma: {_viewModel.Gamma:F1}"),

            // Reset
            (Key.R, ModifierKeys.Shift) => Adjust(() =>
                _viewModel.ResetImageCommand.Execute(null), () => "Ajustes de imagem resetados"),

            _ => false
        };

        if (handled) e.Handled = true;
    }

    private async Task<bool> ProcessPaste(KeyEventArgs e)
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

    private async Task<bool> OpenUrl(KeyEventArgs e, string? title)
    {
        OpenUrlStream(title);
        e.Handled = true;
        return true;
    }

    private void OpenUrlStream(string? title = null)
    {
        var window = new OpenURL(this, title)
        {
            Owner = GetWindow(this)
        };
        window?.Show();
    }
    #endregion

    /// <summary>Dispara quando há movimento do mouse sobre os controles (modo cinema).</summary>
    public event EventHandler? MouseDetected;

    private void OnFilePathChanged()
    {
        Dispatcher.Invoke(() =>
        {
            TitleCenter.Text = PlayerViewModel.FileName;
            Title = string.IsNullOrEmpty(PlayerViewModel.FileName)
                ? "TorrentIsland"
                : $"{PlayerViewModel.FileName} - TorrentIsland";
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
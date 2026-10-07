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

namespace TorrentIsland.Presentation.Player.Windows.Main;

public partial class PlayerWindow : Wpf.Ui.Controls.FluentWindow
{
    #region Fields + Constructor
    private readonly PlayerViewModel _viewModel;
    private readonly ControlsWindow _controls;
    private readonly IDLService _ytDlService;
    private readonly TorrentSearchWindow _torrentSearch;
    private readonly DispatcherTimer _inactivityTimer;
    private readonly DispatcherTimer _osdTimer;
    private readonly System.Timers.Timer _autosaveTimer;
    private int _menusAbertos;
    private int _saving;

    public PlayerWindow(IStreamService streamService,
                        IManagers managers,
                        IDLService ytDlService,
                        ITorrentStatusEvent torrentStatus,
                        IFormattingHelper fb,
                        ITorrentService torrentService)
    {
        Managers = managers;

        InitializeComponent();

        TitleLeft.Text = "TorrentIsland";

        var vlc = new VlcPlayerService();

        _viewModel = new PlayerViewModel(vlc.LibVLC, vlc.MediaPlayer, fb);

        DataContext = _viewModel;

        VideoView.MediaPlayer = vlc.MediaPlayer;

        _controls = new ControlsWindow(_viewModel, this, torrentStatus, torrentService);
        _inactivityTimer = new DispatcherTimer();
        _osdTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(2)
        };

        _autosaveTimer = new System.Timers.Timer
        {
            Interval = TimeSpan.FromSeconds(30).TotalMilliseconds
        };

        _autosaveTimer.Elapsed += async (_, _) =>
        {
            if (Interlocked.CompareExchange(ref _saving, 1, 0) != 0) return;
            try
            {
                if (!Managers.GetEngine()) return;
                await Managers.SaveEngine().ConfigureAwait(false);
            }
            catch (Exception ex) { Log.Salvar($"[Autosave] {ex.Message}"); }
            finally { Interlocked.Exchange(ref _saving, 0); }
        };
        _autosaveTimer.Start();

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

            _inactivityTimer?.Stop();
            _osdTimer?.Stop();

            _controls?.Close();

            _viewModel?.Dispose();
            vlc?.Dispose();
            Log.Salvar("[Shutdown] PlayerWindow fechada.");
            Environment.Exit(0);
        };

        StreamService = streamService;
        _ytDlService = ytDlService;
        _torrentSearch = new TorrentSearchWindow(_viewModel, this, torrentService, torrentStatus);
    }
    #endregion

    #region Properties
    public bool MenuAberto => _menusAbertos > 0;
    public string MediaUrl { get; set; } = "";
    private IStreamService StreamService { get; }
    private IManagers Managers { get; }
    #endregion

    #region Public Methods    
    public void AbrirMenu() => _menusAbertos++;

    public void FecharMenu()
    {
        if (_menusAbertos == 0) return;

        _menusAbertos--;
    }
    public void ReiniciarTimerInatividade(double time = 5)
    {
        if (!_viewModel.IsFullscreen ||
            (MenuAberto && Math.Abs(time - 5) < 0.01)) return;
        _inactivityTimer.Stop();
        _inactivityTimer.Interval = TimeSpan.FromSeconds(time);
        _inactivityTimer.Start();
    }
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
                var media = await MediaSetupAsync(caminhoOuUrl);
                if (media == null) return;
                
                var timeCached = await MediaTimestamp.LoadCache(PlayerViewModel.FilePath);
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
        Media? media = await MediaSetupAsync(file);
        if (media == null) return false;
        _viewModel.AddMedia(media);
        return true;
    }

    private async Task<Media?> MediaSetupAsync(string caminhoOuUrl)
    {
        Media? media = null;
        if (File.Exists(caminhoOuUrl))
        {
            media = new Media(_viewModel.LibVLC, caminhoOuUrl, FromType.FromPath);
            PlayerViewModel.FilePath = caminhoOuUrl;
        }
        else if (Uri.TryCreate(caminhoOuUrl, UriKind.Absolute, out _))
        {
            string? url;
            if (caminhoOuUrl.StartsWith("magnet"))
            {
                var torrents = await Managers.ObterTorrentsAsync();
                if (torrents.Count > 0)
                {
                    PlayerViewModel.FilePath = torrents.Select(v => v.Value.FullPath).FirstOrDefault()!;
                }
                url = caminhoOuUrl;
            }
            else
            {
                url = await _ytDlService.GetStreamingUrl(caminhoOuUrl);
            }

            if (string.IsNullOrEmpty(url))
            {
                MessageBox.Show(
                    $"Url:{caminhoOuUrl} - Falha em obter dados para criação de streaming!",
                    "Player",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return null;
            }  

            media = new Media(_viewModel.LibVLC, url, FromType.FromLocation);
        }
        
        GenerateMkvHash();

        return media;
    }

    private static void GenerateMkvHash()
    {
        var ext = Path.GetExtension(PlayerViewModel.FilePath);
        if (ext != ".mkv") return;        
        KeyGenerator.Hash(PlayerViewModel.FilePath);
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
        ReiniciarTimerInatividade();
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
        _viewModel.Stop();

        if (_viewModel.IsPlaying)
        {
            _viewModel.IsPlaying = false;
            BtnPlayPause.ImageSource = (ImageSource)FindResource("IconPlay");
            BtnPlayPause.Description = "Reproduzir";
        }

        _viewModel.TempoAtualFormatado = "00:00:00";
        _viewModel.TempoTotalFormatado = "00:00:00";
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
        ReiniciarTimerInatividade();
        ReforcarTopmostControles();

        var builder = new PlayerContextMenuBuilder(_viewModel);
        var menu = builder.Build();
        menu.PlacementTarget = OverlayGrid;
        menu.Placement = System.Windows.Controls.Primitives.PlacementMode.MousePoint;
        menu.Closed += (_, _) =>
        {
            FecharMenu();
            ReiniciarTimerInatividade();
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
        ReiniciarTimerOsd();
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
        _viewModel.SubtitleDelayChanged += OnSubtitleDelayChanged;

        // Sincroniza a janela de controles com a janela de vídeo.
        LocationChanged += (_, _) => PosicionarControles();
        SizeChanged += (_, _) => PosicionarControles();
        StateChanged += (_, _) => PosicionarControles();

        // Janela de controles separada (evita o airspace do HWND nativo bloquear os cliques).
        // Owner é atribuído no Loaded (a janela dona precisa estar visível antes).        
        _controls.FullscreenRequested += (_, _) => ToggleFullscreen();
        _controls.ActivityDetected += (_, _) => ReiniciarTimerInatividade();
        _controls.Closed += (_, _) =>
        {
            _inactivityTimer?.Stop();
            Dispatcher.BeginInvoke(new Action(Close), DispatcherPriority.ContextIdle);
        };


        _inactivityTimer.Tick += (_, _) =>
        {
            if (!_viewModel.IsFullscreen || MenuAberto) return;
            HideControls();
        };

        _osdTimer.Tick += (s, e) =>
        {
            OsdNotification.Visibility = Visibility.Collapsed;
            _osdTimer.Stop();
        };
    }

    private void UnsubscribeEvents()
    {
        _controls.FullscreenRequested -= (_, _) => ToggleFullscreen();
        _controls.ActivityDetected -= (_, _) => ReiniciarTimerInatividade();
        OverlayGrid.ContextMenuOpening -= OverlayGrid_ContextMenuOpening;
        PlayerViewModel.FilePathChanged -= OnFilePathChanged;
        _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        _viewModel.OpenFileExternalRequested -= OnOpenFileExternalRequested;
        _viewModel.PlaybackActionRequested -= OnPlaybackActionRequested;
        _viewModel.TorrentSearchRequested -= OnTorrentSearchRequested;
        _viewModel.TorrentQueueRequested -= OnTorrentQueueRequested;
        _viewModel.SubtitleDelayChanged -= OnSubtitleDelayChanged;

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

        bool isOpenBracket = e.Key == Key.OemOpenBrackets || e.Key == Key.Oem5;
        bool isCloseBracket = e.Key == Key.OemCloseBrackets || e.Key == Key.Oem6;
        bool isNotMod = Keyboard.Modifiers == ModifierKeys.None;
        bool isControlKey = Keyboard.Modifiers == ModifierKeys.Control;
        bool isShiftKey = Keyboard.Modifiers == ModifierKeys.Shift;
        bool ExecuteDelay(bool advance, double value)
        { _viewModel.DelaySpuCommand.Execute((advance, value)); return true; }

        bool Adjust(Action action, Func<string> osdMessage)
        {
            action();
            ShowOsd(osdMessage());
            return true;
        }

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
        }

        var handled = (e.Key, isNotMod, isControlKey, isShiftKey) switch
        {
            // Delay de Legenda
            _ when isOpenBracket && isNotMod => ExecuteDelay(false, 0.5),
            _ when isCloseBracket && isNotMod => ExecuteDelay(true, 0.5),
            _ when isOpenBracket && isControlKey => ExecuteDelay(false, 5.0),
            _ when isCloseBracket && isControlKey => ExecuteDelay(true, 5.0),

            (Key.V, _, true, _) => await ProcessPaste(e),
            (Key.L, _, true, _) => await OpenUrl(e),

            // Brilho
            (Key.B, _, true, _) => Adjust(()
                => _viewModel.DecrementBrightness(), () => $"Brilho: {_viewModel.Brightness:P0}"),
            (Key.B, _, _, true) => Adjust(()
                => _viewModel.IncrementBrightness(), () => $"Brilho: {_viewModel.Brightness:P0}"),

            // Contraste
            (Key.C, _, true, _) => Adjust(()
                => _viewModel.DecrementContrast(), () => $"Contraste: {_viewModel.Contrast:P0}"),
            (Key.C, _, _, true) => Adjust(()
                => _viewModel.IncrementContrast(), () => $"Contraste: {_viewModel.Contrast:P0}"),

            // Matiz
            (Key.H, _, true, _) => Adjust(()
                => _viewModel.DecrementHue(), () => $"Matiz: {_viewModel.Hue}"),
            (Key.H, _, _, true) => Adjust(()
                => _viewModel.IncrementHue(), () => $"Matiz: {_viewModel.Hue}"),

            // Saturação
            (Key.S, _, true, _) => Adjust(()
                => _viewModel.DecrementSaturation(), () => $"Saturação: {_viewModel.Saturation:P0}"),
            (Key.S, _, _, true) => Adjust(()
                => _viewModel.IncrementSaturation(), () => $"Saturação: {_viewModel.Saturation:P0}"),

            // Gamma
            (Key.G, _, true, _) => Adjust(()
                => _viewModel.DecrementGamma(), () => $"Gamma: {_viewModel.Gamma:F1}"),
            (Key.G, _, _, true) => Adjust(()
                => _viewModel.IncrementGamma(), () => $"Gamma: {_viewModel.Gamma:F1}"),

            // Reset
            (Key.R, _, _, true) => Adjust(()
                => _viewModel.ResetImageCommand.Execute(null), () => "Ajustes de imagem resetados"),

            // Nenhuma combinação correspondida
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

    private async Task<bool> OpenUrl(KeyEventArgs e)
    {
        var window = new OpenURL(this)
        {
            Owner = GetWindow(this)
        };
        window?.Show();
        e.Handled = true;
        return true;
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
    private void OnSubtitleDelayChanged(object? sender, EventArgs e)
    {
        OsdNotification.Visibility = Visibility.Visible;
        ReiniciarTimerOsd();
    }

    protected override void OnClosed(EventArgs e)
    {
        _autosaveTimer.Stop();

        Task.Run(async () =>
        {
            try
            {
                if (!Managers.GetEngine()) return;
                await Managers.SaveEngine();
            }
            catch (Exception ex) { Log.Salvar($"[Shutdown] {ex.Message}"); }

            _autosaveTimer?.Dispose();
        });

        base.OnClosed(e);
    }
}
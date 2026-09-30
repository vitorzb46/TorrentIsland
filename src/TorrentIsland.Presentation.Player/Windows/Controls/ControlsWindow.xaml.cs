using Microsoft.Win32;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using TorrentIsland.Application.DTOs;
using TorrentIsland.Application.Interfaces;
using TorrentIsland.Infrastructure.Logging;
using TorrentIsland.Presentation.Player.Common;
using TorrentIsland.Presentation.Player.ViewModel;
using TorrentIsland.Presentation.Player.Windows.Main;
using TorrentIsland.Presentation.Player.Windows.WebSearch;
using Wpf.Ui.Controls;
using Button = System.Windows.Controls.Button;
using MenuItem = System.Windows.Controls.MenuItem;

namespace TorrentIsland.Presentation.Player.Windows.Controls;

public partial class ControlsWindow : Window, IDisposable
{
    private readonly PlayerViewModel _viewModel;
    private readonly PlayerWindow _playerWindow;
    private readonly ITorrentStatusEvent _torrentStatus;
    private readonly ITorrentService _torrent;
    private static TorrentSearchWindow? _searchWindow;

    private bool _isSeeking { get; set; }
    private bool _isDisposed { get; set; }

    private ToolTip? _timelineTooltip;

    /// <summary>Dispara quando o usuário alternar tela cheia (a janela de vídeo executa).</summary>
    public event EventHandler? FullscreenRequested;
    /// <summary>Dispara quando há movimento do mouse sobre os controles (modo cinema).</summary>
    public event EventHandler? ActivityDetected;

    public ControlsWindow(PlayerViewModel viewModel,
                          PlayerWindow playerWindow,
                          ITorrentStatusEvent torrentStatus,
                          ITorrentService torrent)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _playerWindow = playerWindow;
        _torrentStatus = torrentStatus;
        _torrent = torrent;
        DataContext = viewModel;
        TorrentStatusButton.Content = new SymbolIcon { Symbol = SymbolRegular.Globe32 };
        ConfigurarToolTips();

        _timelineTooltip = Utils.ToolTipDesign(
            "00:00",
            TimelineSlider,
            horiOffset: 0,
            vertOffset: -42);
        _timelineTooltip.Placement = PlacementMode.Relative;
        _timelineTooltip.PlacementTarget = TimelineSlider;

        TorrentStatusFlyout.Loaded += (s, e) =>
        {
            if (TorrentStatusFlyout.Template.FindName("PART_Popup", TorrentStatusFlyout) is Popup popup)
            {
                popup.PlacementTarget = TorrentStatusButton;
                popup.CustomPopupPlacementCallback = PlaceFlyoutAboveCentered;
            }
        };

        DependencyPropertyDescriptor
            .FromProperty(Flyout.IsOpenProperty, typeof(Flyout))
            .AddValueChanged(TorrentStatusFlyout, OnFlyoutIsOpenChanged);
    }

    #region Timeline Slider
    private void TimelineSlider_PreviewMouseDown(object sender, MouseButtonEventArgs e) => _isSeeking = true;

    private void TimelineSlider_PreviewMouseUp(object sender, MouseButtonEventArgs e)
    {
        _isSeeking = false;

        if (_timelineTooltip is not null)
            _timelineTooltip.IsOpen = false;

        var tempoAlvo = TimeSpan.FromMilliseconds(TimelineSlider.Value);
        _viewModel.SeekTo(tempoAlvo);
    }

    private void TimelineSlider_MouseMove(object sender, MouseEventArgs e)
    {
        if (!_isSeeking || _viewModel is null || _timelineTooltip is null) return;

        double ms = Math.Max(0, Math.Min(TimelineSlider.Value, TimelineSlider.Maximum));
        var tempo = TimeSpan.FromMilliseconds(ms);

        string texto = tempo.TotalHours >= 1
            ? tempo.ToString(@"hh\:mm\:ss")
            : tempo.ToString(@"mm\:ss");

        if (_timelineTooltip.Content is Border border &&
            border.Child is System.Windows.Controls.TextBlock tb)
        {
            tb.Text = texto;
        }

        _timelineTooltip.HorizontalOffset = e.GetPosition(TimelineSlider).X - 28.0;
        _timelineTooltip.VerticalOffset = -42.0;

        if (!_timelineTooltip.IsOpen)
            _timelineTooltip.IsOpen = true;
    }
    #endregion

    #region Coluna 1 (Botoes de controle)
    private void RetrocederButton_Click(object sender, RoutedEventArgs e) => _viewModel.RetrocederTempo();

    private void AvancarButton_Click(object sender, RoutedEventArgs e) => _viewModel.AvancarTempo();

    private void PlayPauseButton_Click(object sender, RoutedEventArgs e) => _viewModel.TogglePlay();

    private void MuteButton_Click(object sender, RoutedEventArgs e) => _viewModel.ToggleMute();

    private void VolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (Math.Abs(e.NewValue - e.OldValue) < 0.001) return;
        if (_viewModel.IsMuted && e.NewValue > 0)
        {
            _viewModel.SetMute(false);
        }
    }
    #endregion

    #region Coluna 2 (Configurações e legendas)
    private async void LoadMediaButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            await ProcessarMidiaAsync();
        }
        catch (Exception ex)
        {
            Log.Salvar($"Erro ao carregar mídia: {ex.Message}");
        }
    }

    private void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        _playerWindow.ReiniciarTimerInatividade();
        if (sender is not Button btn) return;
        ConfigMenu.DataContext = DataContext;
        ConfigMenu.PlacementTarget = btn;
        ConfigMenu.Placement = PlacementMode.Top;
        ConfigMenu.HorizontalOffset = -120;
        ConfigMenu.VerticalOffset = -25;
        ConfigMenu.IsOpen = true;
    }

    private void FullscreenButton_Click(object sender, RoutedEventArgs e)
    {
        FullscreenRequested?.Invoke(this, EventArgs.Empty);
    }

    private void SubtitleMenuGroup_Click(object sender, RoutedEventArgs e)
    {
        Log.Salvar("SubtitleMenuGroup_Click");
        if (e.OriginalSource is MenuItem { Header: TrackItem track } dObject)
        {
            Log.Salvar("SubtitleMenuGroup_Click dentro do if");
            _playerWindow.ReiniciarTimerInatividade();
            _viewModel.SelectSubtitleTrack(track.Id);

            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                var playerWindow = System.Windows.Application.Current.Windows
                    .OfType<PlayerWindow>()
                    .FirstOrDefault();

                playerWindow?.VideoView.InvalidateVisual();
            });
            FecharMenuConfiguracoes(dObject);
            return;
        }
    }

    private void AudioMenuGroup_Click(object sender, RoutedEventArgs e)
    {
        if (e.OriginalSource is MenuItem { Header: TrackItem track } dObject)
        {
            _playerWindow.ReiniciarTimerInatividade();
            _viewModel.SelectAudioTrack(track.Id);
            FecharMenuConfiguracoes(dObject);
            return;
        }
    }

    private async void TorrentMenuItem_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            await ProcessarTorrentAsync();
        }
        catch (Exception ex)
        {
            Log.Salvar($"Erro ao processar arquivo torrent: {ex.Message}");
        }
    }

    public void BuscarTorrentMenuItem_Click(object? sender, EventArgs e)
    {
        _viewModel.SetPause(true);

        if (_searchWindow != null)
        {
            _searchWindow?.Show();
        }
        else
        {
            _searchWindow = new TorrentSearchWindow(_viewModel, _playerWindow, _torrent, _torrentStatus)
            {
                Owner = GetWindow(this),
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                ExtendsContentIntoTitleBar = true,
                WindowCornerPreference = WindowCornerPreference.Round
            };
            _searchWindow?.Show();
        }
    }

    private void TorrentStatusButton_Click(object sender, RoutedEventArgs e)
    {
        TorrentStatusFlyout.IsOpen = !TorrentStatusFlyout.IsOpen;
    }
    #endregion


    // --- Modo cinema: qualquer movimento do mouse na janela de controles reinicia o timer de inatividade ---
    private void Window_MouseMove(object sender, MouseEventArgs e)
    {
        if (_viewModel.IsFullscreen)
        {
            ActivityDetected?.Invoke(this, EventArgs.Empty);
        }
    }

    private void ConfigurarToolTips()
    {
        PlayPauseButton.ToolTip = Utils.ToolTipDesign("Play/Pause", PlayPauseButton, horiOffset: -32);
        RetrocederButton.ToolTip = Utils.ToolTipDesign("Retroceder", RetrocederButton, horiOffset: -32);
        AvancarButton.ToolTip = Utils.ToolTipDesign("Avançar", AvancarButton, horiOffset: -25);
        MuteButton.ToolTip = Utils.ToolTipDesign("Ativar / Desativar som", MuteButton, horiOffset: -65);
        SettingsButton.ToolTip = Utils.ToolTipDesign("Configurações", SettingsButton, horiOffset: -38);
        FullscreenButton.ToolTip = Utils.ToolTipDesign("Tela cheia", FullscreenButton, horiOffset: -28);
        LoadMediaButton.ToolTip = Utils.ToolTipDesign("Carregar legenda ou vídeo", LoadMediaButton, horiOffset: -75);
    }

    public async Task ProcessarMidiaAsync()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "Vídeos (*.mp4;*.mkv;*.webm;*.avi)|*.mp4;*.mkv;*.webm;*.avi|" +
                 "Todos os arquivos (*.*)|*.*",
            Title = "Carregar mídia externa"
        };

        if (dialog.ShowDialog(this) == true)
        {
            string extensao = Path.GetExtension(dialog.FileName).ToLowerInvariant();

            if (Utils.ExtensoesVideo.Contains(extensao))
            {
                _viewModel.LoadingMessage = "Carregando mídia...";
                await _playerWindow.CarregarMidiaAsync(dialog.FileName);
            }
        }
    }

    public async Task ProcessarLegendaExternaAsync()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "Legendas (*.srt;*.vtt;*.ssa;*.ass)|*.srt;*.vtt;*.ssa;*.ass|" +
                 "Todos os arquivos (*.*)|*.*",
            Title = "Carregar legenda externa"
        };

        if (dialog.ShowDialog(this) == true)
        {
            string extensao = Path.GetExtension(dialog.FileName).ToLowerInvariant();
            if (Utils.ExtensoesSubs.Contains(extensao))
            {
                _viewModel.LoadExternalSubtitle(dialog.FileName);
            }
        }
    }

    public async Task ProcessarTorrentAsync()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "Torrent (*.torrent)|*.torrent|" +
                 "Todos os arquivos (*.*)|*.*",
            Title = "Carregar arquivo torrent"
        };

        if (dialog.ShowDialog(this) == true)
        {
            string extensao = Path.GetExtension(dialog.FileName).ToLowerInvariant();
            if (Utils.ExtensaoTorrent.Contains(extensao))
            {
                await _playerWindow.CarregarStreamTorrentAsync(dialog.FileName);
            }
        }
    }

    /// <summary>
    /// Método auxiliar para encontrar o ContextMenu ancestral e fechá-lo de forma segura.
    /// </summary>
    private void FecharMenuConfiguracoes(DependencyObject elemento)
    {
        var atual = elemento;

        // Sobe na árvore de elementos até encontrar o ContextMenu pai
        while (atual != null && atual is not System.Windows.Controls.ContextMenu)
        {
            // Tenta pegar o pai lógico ou o pai visual usando um cast seguro
            atual = (atual as FrameworkElement)?.Parent ?? VisualTreeHelper.GetParent(atual);
            atual = (atual as FrameworkElement)?.Parent ?? VisualTreeHelper.GetParent(atual);
        }

        if (atual is ContextMenu menu)
        {
            menu.IsOpen = false;
        }

        _viewModel.SetReset();
    }

    private CustomPopupPlacement[] PlaceFlyoutAboveCentered(Size popupSize,
                                                            Size targetSize,
                                                            Point offset)
    {
        double x = (targetSize.Width - popupSize.Width) / 2;
        double y = -popupSize.Height;

        return
        [
            new CustomPopupPlacement(new Point(x, y), PopupPrimaryAxis.Horizontal)
        ];
    }

    private void OnFlyoutIsOpenChanged(object? sender, EventArgs e)
    {
        if (TorrentStatusFlyout.IsOpen)
            _torrentStatus.Start();
        else
            _torrentStatus.Stop();
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        Dispose();
    }

    public void Dispose()
    {
        if (_isDisposed) return;

        DependencyPropertyDescriptor
            .FromProperty(Flyout.IsOpenProperty, typeof(Flyout))
            .RemoveValueChanged(TorrentStatusFlyout, OnFlyoutIsOpenChanged);

        if (_torrentStatus.Timer != null)
        {
            _torrentStatus.Dispose();
        }

        _isDisposed = true;

        GC.SuppressFinalize(this);
    }
}

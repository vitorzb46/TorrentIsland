using Microsoft.Win32;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using TorrentIsland.Application.DTOs;
using TorrentIsland.Presentation.Player.Common;
using TorrentIsland.Presentation.Player.ViewModel;
using TorrentIsland.Presentation.Player.Windows.Main;
using TorrentIsland.Presentation.Player.Windows.WebSearch;
using Wpf.Ui.Controls;
using Button = System.Windows.Controls.Button;
using MenuItem = System.Windows.Controls.MenuItem;

namespace TorrentIsland.Presentation.Player.Windows.Controls;

public partial class ControlsWindow : Window
{
    private readonly PlayerViewModel _viewModel;
    private readonly PlayerWindow _playerWindow;

    private bool _isSeeking { get; set; }

    /// <summary>Dispara quando o usuário alternar tela cheia (a janela de vídeo executa).</summary>
    public event EventHandler? FullscreenRequested;
    /// <summary>Dispara quando há movimento do mouse sobre os controles (modo cinema).</summary>
    public event EventHandler? ActivityDetected;

    public ControlsWindow(PlayerViewModel viewModel, PlayerWindow playerWindow)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _playerWindow = playerWindow;
        DataContext = viewModel;
        ConfigurarToolTips();
    }

    #region Timeline Slider
    private void TimelineSlider_PreviewMouseDown(object sender, MouseButtonEventArgs e) => _isSeeking = true;

    private void TimelineSlider_PreviewMouseUp(object sender, MouseButtonEventArgs e)
    {
        _isSeeking = false;
        if (TimelineSlider.ToolTip is ToolTip tooltip)
        {
            tooltip.IsOpen = false;
        }

        var tempoAlvo = TimeSpan.FromMilliseconds(TimelineSlider.Value);

        _viewModel.SeekTo(tempoAlvo);
    }

    private void TimelineSlider_MouseMove(object sender, MouseEventArgs e)
    {
        // Só formata e mostra se o usuário estiver ativamente arrastando/clicando
        if (_isSeeking && _viewModel != null)
        {
            double milissegundosAlvo = TimelineSlider.Value;

            milissegundosAlvo = Math.Max(0, Math.Min(milissegundosAlvo, TimelineSlider.Maximum));

            TimeSpan tempo = TimeSpan.FromMilliseconds(milissegundosAlvo);

            string tempoFormatado = tempo.TotalHours >= 1
            ? tempo.ToString(@"hh\:mm\:ss")
            : tempo.ToString(@"mm\:ss");

            TimelineSlider.ToolTip = Utils.ToolTipDesign(
                tempoFormatado,
                TimelineSlider,
                e.GetPosition(TimelineSlider).X - 28.0,
                -42.0);

            var tip = TimelineSlider.ToolTip as ToolTip;
            tip?.IsOpen = true;
        }
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
            await ProcessarEscolhaDeArquivoAsync();
        }
        catch (Exception ex)
        {
            Log.Salvar($"Erro ao carregar legenda: {ex.Message}");
        }
    }

    private void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        Log.Salvar("SettingsButton_Click aberto");
        _playerWindow.ReiniciarTimerInatividade();
        if (sender is not Button btn) return;
        ConfigMenu.DataContext = DataContext;
        ConfigMenu.PlacementTarget = btn;
        ConfigMenu.Placement = PlacementMode.Top;
        ConfigMenu.HorizontalOffset = -120;
        ConfigMenu.VerticalOffset = -25;
        ConfigMenu.IsOpen = true;
        Log.Salvar("SettingsButton_Click fechado");
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

    private void BuscarTorrentMenuItem_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.SetPause(true);
        var searchWindow = new TorrentSearchWindow
        {
            Owner = GetWindow(this),
            WindowStartupLocation = WindowStartupLocation.CenterScreen,
            ExtendsContentIntoTitleBar = true,
            WindowCornerPreference = WindowCornerPreference.Round
        };

        if (searchWindow.ShowDialog() == true)
        {
            string? linkSelecionado = searchWindow.LinkSelecionado;
            if (!string.IsNullOrEmpty(linkSelecionado))
            {
                _ = _playerWindow.CarregarStreamTorrentAsync(linkSelecionado);
            }
        }
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

    private async Task ProcessarEscolhaDeArquivoAsync()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "Arquivos Suportados (*.srt;*.vtt;*.ssa;*.ass;*.mp4;*.mkv;*.avi)|*.srt;*.vtt;*.ssa;*.ass;*.mp4;*.mkv;*.avi|" +
                 "Legendas (*.srt;*.vtt;*.ssa;*.ass)|*.srt;*.vtt;*.ssa;*.ass|" +
                 "Vídeos (*.mp4;*.mkv;*.avi)|*.mp4;*.mkv;*.avi|" +
                 "Todos os arquivos (*.*)|*.*",
            Title = "Carregar legenda externa"
        };

        if (dialog.ShowDialog(this) == true)
        {
            string extensao = Path.GetExtension(dialog.FileName).ToLowerInvariant();

            if (Utils.ExtensoesVideo.Contains(extensao))
            {
                _viewModel.LoadingMessage = "Carregando mídia...";
                await _playerWindow.CarregarMidiaAsync(dialog.FileName);
            }
            else if (Utils.ExtensoesSubs.Contains(extensao))
            {
                _viewModel.LoadExternalSubtitle(dialog.FileName);
            }
        }
    }

    private async Task ProcessarTorrentAsync()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "Arquivos Torrent (*.torrent)|*.torrent",
            Title = "Carregar arquivo torrent"
        };
        if (dialog.ShowDialog(this) == true)
        {
            await _playerWindow.CarregarStreamTorrentAsync(dialog.FileName);
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
}

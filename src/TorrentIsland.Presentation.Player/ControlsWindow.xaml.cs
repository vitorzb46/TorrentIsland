using Microsoft.Win32;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace TorrentIsland.Presentation.Player;

public partial class ControlsWindow : Window
{
    private readonly PlayerViewModel _viewModel;
    private bool _isSeeking { get; set; }

    /// <summary>Dispara quando o usuário alternar tela cheia (a janela de vídeo executa).</summary>
    public event EventHandler? FullscreenRequested;
    /// <summary>Dispara quando há movimento do mouse sobre os controles (modo cinema).</summary>
    public event EventHandler? ActivityDetected;

    public ControlsWindow(PlayerViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;
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

            TimelineSlider.ToolTip = ToolTipDesign(
                tempoFormatado,
                new ToolTip(),
                GetTimelineSlider(),
                e.GetPosition(TimelineSlider).X - 28.0,
                -42.0);
        }
    }
    #endregion

    #region Coluna 1 (Botoes de controle)
    private void RetrocederButton_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.RetrocederTempo();
        RetrocederButton.ToolTip = ToolTipDesign("Retroceder 5s", new ToolTip(), RetrocederButton, -55.0, -35.0);
    }

    private void AvancarButton_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.AvancarTempo();
        AvancarButton.ToolTip = ToolTipDesign("Avancar 5s", new ToolTip(), AvancarButton, -55.0, -35.0);
    }

    private void PlayPauseButton_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.TogglePlay();
        PlayPauseButton.Content = _viewModel.IsPlaying ? "⏸" : "▶";
        PlayPauseButton.ToolTip = ToolTipDesign("Pausar / Reproduzir", new ToolTip(), PlayPauseButton, -55.0, -35.0);
    }

    private void MuteButton_Click(object sender, RoutedEventArgs e)
    {
        Log.Salvar("MuteButton_Click");
        _viewModel.ToggleMute();
        MuteButton.Content = _viewModel.IsMuted ? "🔇" : "🔊";

        MuteButton.ToolTip = ToolTipDesign("Mutar / Desmutar", new ToolTip(), MuteButton, -55.0, -35.0);
    }

    private void VolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        Log.Salvar("VolumeSlider_ValueChanged");
        if (_viewModel.IsMuted && _viewModel.Volume > 0)
        {
            _viewModel.SetMute(false);
            MuteButton.Content = "🔊";
            Log.Salvar("MuteButton desativado");
        }
    }
    #endregion

    #region Coluna 2 (Configurações e legendas)
    private void LoadSubtitleButton_Click(object sender, RoutedEventArgs e)
    {
        LoadSubtitleButton.ToolTip = ToolTipDesign("Carregar legenda ou vídeo", new ToolTip(), LoadSubtitleButton, -55.0, -35.0);

        Log.Salvar("LoadSubtitleButton_Click");
        var dialog = new OpenFileDialog
        {
            Filter = "Legendas (*.srt;*.vtt)|*.srt;*.vtt|Todos os arquivos (*.*)|*.*",
            Title = "Carregar legenda externa"
        };

        if (dialog.ShowDialog(this) == true)
        {
            Log.Salvar("Legenda {dialog.FileName} carregada!");
            _viewModel.LoadExternalSubtitle(dialog.FileName);
        }
    }

    private void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        SettingsButton.ToolTip = ToolTipDesign("Settings", new ToolTip(), SettingsButton, -55.0, -35.0);
        if (sender is not Button btn) return;
        ConfigMenu.DataContext = this.DataContext;
        ConfigMenu.PlacementTarget = btn;
        ConfigMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Top;
        ConfigMenu.HorizontalOffset = -120;
        ConfigMenu.VerticalOffset = -25;
        ConfigMenu.IsOpen = true;
    }

    private void FullscreenButton_Click(object sender, RoutedEventArgs e)
    {
        FullscreenRequested?.Invoke(this, EventArgs.Empty);
        FullscreenButton.ToolTip = ToolTipDesign("Modo cinema", new ToolTip(), FullscreenButton, -55.0, -35.0);
    }   

    private void SubtitleMenuGroup_Click(object sender, RoutedEventArgs e)
    {
        if (e.OriginalSource is MenuItem { Header: TrackItem track })
        {
            _viewModel.SelectSubtitleTrack(track.Id);
            _viewModel.SetTime();

            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                var playerWindow = System.Windows.Application.Current.Windows
                    .OfType<PlayerWindow>()
                    .FirstOrDefault();

                playerWindow?.VideoView.InvalidateVisual();
            });
            return;
        }
    }

    private void AudioMenuGroup_Click(object sender, RoutedEventArgs e)
    {
        if (e.OriginalSource is MenuItem { Header: TrackItem track })
        {
            _viewModel.SelectAudioTrack(track.Id);
            return;
        }
    }   

    private void TorrentMenuItem_Click(object sender, RoutedEventArgs e)
    {
        Log.Salvar("TorrentMenuItem_Click");
        var dialog = new OpenFileDialog
        {
            Filter = "Arquivos Torrent (*.torrent)|*.torrent",
            Title = "Carregar arquivo torrent"
        };
        if (dialog.ShowDialog(this) == true)
        {
            Log.Salvar($"Torrent {dialog.FileName} aberto!");
            _viewModel.LoadExternalTorrent(dialog.FileName);
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

    private UIElement GetTimelineSlider()
    {
        return TimelineSlider;
    }

    // Design da ToolTip do TimelineSlider
    private ToolTip ToolTipDesign(string text, ToolTip tooltip, UIElement element, double? horiOffset = null, double? vertOffset = null)
    {
        tooltip.IsOpen = true;

        tooltip.Background = Brushes.Transparent;
        tooltip.BorderBrush = Brushes.Transparent;
        tooltip.BorderThickness = new Thickness(0);
        tooltip.Padding = new Thickness(0);

        var textoBloco = new TextBlock
        {
            Text = text,
            Foreground = Brushes.White,
            FontSize = 14,
            FontWeight = FontWeights.SemiBold,
            FontFamily = new FontFamily("Segoe UI Variable Display, Bahnschrift"),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };

        var containerEscuro = new Border
        {
            Background = (Brush)new BrushConverter().ConvertFromString("#E60A0A0A")!, // Fundo escuro com opacidade
            BorderBrush = (Brush)new BrushConverter().ConvertFromString("#2D323F")!, // Cor da borda
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),

            // AJUSTE DO PADDING: (Esquerda, Topo, Direita, Fundo)
            Padding = new Thickness(14, 6, 14, 6),

            Child = textoBloco // Injeta o texto dentro do container
        };

        tooltip.Content = containerEscuro;

        tooltip.PlacementTarget = element;
        tooltip.Placement = System.Windows.Controls.Primitives.PlacementMode.Relative;

        if (horiOffset != null)
        {
            tooltip.HorizontalOffset = horiOffset ?? 0;
            tooltip.VerticalOffset = vertOffset ?? 0;
        }

        return tooltip;
    }
}

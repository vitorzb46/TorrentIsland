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

            if (TimelineSlider.ToolTip == null || TimelineSlider.ToolTip is not System.Windows.Controls.ToolTip)
            {
                TimelineSlider.ToolTip = new ToolTip();
            }

            var tooltip = (ToolTip)TimelineSlider.ToolTip;
            tooltip.Background = Brushes.Transparent;
            tooltip.BorderBrush = Brushes.Transparent;
            tooltip.BorderThickness = new Thickness(0);
            tooltip.Padding = new Thickness(0);

            var textoTempo = new TextBlock
            {
                Text = tempoFormatado,
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

                Child = textoTempo // Injeta o texto dentro do container
            };

            tooltip.Content = containerEscuro;

            tooltip.PlacementTarget = TimelineSlider;
            tooltip.Placement = System.Windows.Controls.Primitives.PlacementMode.Relative;

            double mouseX = e.GetPosition(TimelineSlider).X;

            tooltip.HorizontalOffset = mouseX - 28;
            tooltip.VerticalOffset = -42;

            tooltip.IsOpen = true;
        }
    }
    #endregion

    #region Coluna 1 (Botoes de controle)
    private void RetrocederButton_Click(object sender, RoutedEventArgs e) => _viewModel.RetrocederTempo();

    private void AvancarButton_Click(object sender, RoutedEventArgs e) => _viewModel.AvancarTempo();

    private void PlayPauseButton_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.TogglePlay();
        PlayPauseButton.Content = _viewModel.IsPlaying ? "⏸" : "▶";
    }

    private void MuteButton_Click(object sender, RoutedEventArgs e)
    {
        Log.Salvar("MuteButton_Click");
        _viewModel.ToggleMute();
        MuteButton.Content = _viewModel.IsMuted ? "🔇" : "🔊";
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
    private void FullscreenButton_Click(object sender, RoutedEventArgs e) => FullscreenRequested?.Invoke(this, EventArgs.Empty);

    private void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn) return;
        ConfigMenu.DataContext = this.DataContext;
        ConfigMenu.PlacementTarget = btn;
        ConfigMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Top;
        ConfigMenu.HorizontalOffset = -120;
        ConfigMenu.VerticalOffset = -25;
        ConfigMenu.IsOpen = true;
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

    private void LoadSubtitleButton_Click(object sender, RoutedEventArgs e)
    {
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
}

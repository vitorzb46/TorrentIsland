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

    // --- Timeline (proteção contra loop) ---
    private void TimelineSlider_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        _isSeeking = true;
    }

    private void TimelineSlider_PreviewMouseUp(object sender, MouseButtonEventArgs e)
    {
        _isSeeking = false;
        if (TimelineSlider.ToolTip is ToolTip tooltip)
        {
            tooltip.IsOpen = false;
        }

        var tempoAlvo = TimeSpan.FromMilliseconds(TimelineSlider.Value);
        _viewModel.SeekTo2(tempoAlvo);
    }

    private void TimelineSlider_MouseMove(object sender, MouseEventArgs e)
    {
        // Só formata e mostra se o usuário estiver ativamente arrastando/clicando
        if (_isSeeking && _viewModel != null)
        {
            double proporcaoMouse = e.GetPosition(TimelineSlider).X / TimelineSlider.ActualWidth;
            double milissegundosAlvo = proporcaoMouse * TimelineSlider.Maximum;

            milissegundosAlvo = Math.Max(0, Math.Min(milissegundosAlvo, TimelineSlider.Maximum));

            TimeSpan tempo = TimeSpan.FromMilliseconds(milissegundosAlvo);

            string tempoFormatado = tempo.TotalHours >= 1
            ? tempo.ToString(@"hh\:mm\:ss")
            : tempo.ToString(@"mm\:ss");

            if (TimelineSlider.ToolTip == null || !(TimelineSlider.ToolTip is ToolTip))
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

    // --- Manipulador de clique para os sub-itens do menu de Áudio ---
    //private void AudioMenuItem_Click(object sender, RoutedEventArgs e)
    //{
    //    if (sender is MenuItem menuItem && menuItem.DataContext is TrackItem track)
    //    {
    //        Log.Salvar($"ContextMenu Audio: faixa {track.Id} ({track.Name})");

    //        _viewModel.SelectAudioTrack(track.Id);

    //        FecharMenuConfiguracoes(menuItem);
    //    }
    //}

    // --- Manipulador de clique para os sub-itens do menu de Legendas ---
    //private void SubtitleMenuItem_Click(object sender, RoutedEventArgs e)
    //{
    //    if (sender is MenuItem menuItem && menuItem.DataContext is TrackItem track)
    //    {
    //        Log.Salvar($"ContextMenu Subtitle: faixa {track.Id} ({track.Name})");
    //        _viewModel.SelectSubtitleTrack(track.Id);

    //        FecharMenuConfiguracoes(menuItem);
    //    }
    //}

    private void SubtitleMenuGroup_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (e.OriginalSource is MenuItem menuItemReal)
            {
                if (menuItemReal.Header is TrackItem track)
                {

                    _viewModel.SelectSubtitleTrack(track.Id);

                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        var playerWindow = System.Windows.Application.Current.Windows
                            .OfType<PlayerWindow>()
                            .FirstOrDefault();

                        if (playerWindow != null)
                        {
                            playerWindow.VideoView.InvalidateVisual();
                            Log.Salvar("WPF: Redesenho forçado no componente VideoView da PlayerWindow.");
                        }
                    });
                    return;
                }
            }

            Log.Salvar("Aviso: O cabeçalho da linha clicada não continha um objeto TrackItem válido.");
        }
        catch (Exception ex)
        {
            Log.Salvar($"Erro no clique de legenda: {ex.Message}");
        }
        finally
        {
            // Garante que o painel de controles escuro do WPF-UI continue aberto na tela
            e.Handled = true;
        }
    }

    private void AudioMenuGroup_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Log.Salvar("=== INTERCEPTADO CLIQUE GLOBAL DE ÁUDIO ===");

            if (e.OriginalSource is MenuItem menuItemReal)
            {
                // Mesma extração direta aplicada ao canal de áudio do filme
                if (menuItemReal.Header is TrackItem track)
                {
                    Log.Salvar($"SUCESSO TOTAL DA SÉRIE: Ativando Áudio ID: {track.Id} | Nome: {track.Name}");
                    _viewModel.SelectAudioTrack(track.Id);
                    return;
                }
            }
        }
        catch (Exception ex)
        {
            Log.Salvar($"Erro no clique de áudio: {ex.Message}");
        }
        finally
        {
            e.Handled = true;
        }
    }



    // --- Botões ---
    // Habilitar ou desabilitar AudioTrack é mais eficiente. Altero se der problema no futuro
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

    private void PlayPauseButton_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.TogglePlay();
        PlayPauseButton.Content = _viewModel.IsPlaying ? "⏸" : "▶";
    }
    private bool IsFullscreen = false;
    private void FullscreenButton_Click(object sender, RoutedEventArgs e)
    {
        FullscreenRequested?.Invoke(this, EventArgs.Empty);
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

    // --- Modo cinema: qualquer movimento do mouse na janela de controles reinicia o timer de inatividade ---
    private void Window_MouseMove(object sender, MouseEventArgs e)
    {
        if (_viewModel.IsFullscreen)
        {
            // Sinaliza atividade para a janela de vídeo reiniciar o timer de inatividade.
            ActivityDetected?.Invoke(this, EventArgs.Empty);
        }
    }

    //private void SettingsButton_Click(object sender, RoutedEventArgs e)
    //{
    //    if (sender is Button btn && btn.ContextMenu != null)
    //    {
    //        btn.ContextMenu.PlacementTarget = btn;
    //        btn.ContextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Top;
    //        btn.ContextMenu.HorizontalOffset = -120;
    //        btn.ContextMenu.VerticalOffset = -25;
    //        btn.ContextMenu.IsOpen = true;
    //    }
    //}

    private void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Log.Salvar("SettingsButton_Click disparado.");

            if (sender is Button btn)
            {
                ConfigMenu.DataContext = this.DataContext;

                ConfigMenu.PlacementTarget = btn;

                ConfigMenu.IsOpen = true;

                Log.Salvar("ContextMenu aberto e DataContext injetado via C# com sucesso!");
            }
        }
        catch (Exception ex)
        {
            Log.Salvar($"Erro ao abrir menu de configurações: {ex.Message}");
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
        }

        if (atual is ContextMenu menu)
        {
            menu.IsOpen = false;
        }
    }

    private void TorrentMenuItem_Click(object sender, RoutedEventArgs e)
    {
        try
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
        catch (Exception ex)
        {
            Log.Salvar(ex.Message);
        }

    }
}

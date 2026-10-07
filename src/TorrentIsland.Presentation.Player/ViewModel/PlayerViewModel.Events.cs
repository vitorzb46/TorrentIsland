using LibVLCSharp.Shared;
using System.ComponentModel;
using System.Windows.Threading;
using TorrentIsland.Application.Medias.Events;
using TorrentIsland.Presentation.Player.Common;

namespace TorrentIsland.Presentation.Player.ViewModel;

public partial class PlayerViewModel
{
    private DispatcherTimer _loadingDebounce;

    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary> Disparado toda vez que um novo valor é setado em FilePath </summary>
    public static event Action? FilePathChanged;

    /// <summary>Disparado quando o usuário pede para abrir um arquivo externo.</summary>
    public event EventHandler<ExternalMediaEventArgs>? OpenFileExternalRequested;

    /// <summary>Disparado quando o usuário aciona uma tecla de controle.</summary>
    public event EventHandler<PlaybackEventArgs>? PlaybackActionRequested;

    /// <summary>Disparado quando o usuário aciona o atalho da janela de torrents.</summary>
    public event EventHandler? TorrentSearchRequested;

    /// <summary>Disparado quando o usuário aciona o atalho da janela de torrents.</summary>
    public event EventHandler? TorrentQueueRequested;

    public event EventHandler? SubtitleDelayChanged;

    /// <summary>Disparado quando o usuário aciona janela para url de streaming</summary>
    public event EventHandler? OpenUrlStreamRequested;


    private void LoadingDebounce()
    {
        _loadingDebounce = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(500)
        };
        _loadingDebounce.Tick += (_, _) =>
        {
            _loadingDebounce.Stop();
            IsLoading = false;
        };
    }

    private void OnPlayerBuffering(object? sender, MediaPlayerBufferingEventArgs e)
    {
        System.Windows.Application.Current.Dispatcher.BeginInvoke(() =>
        {
            if (IsOpening) return;

            if (e.Cache < 100 && IsPlaying)
            {
                IsLoading = true;
                _loadingDebounce.Stop();
                _loadingDebounce.Start();
            }
            else if (e.Cache >= 100)
            {
                _loadingDebounce.Stop();
                IsLoading = false;
            }
        });
    }
    private void OnPositionChanged(object? sender, MediaPlayerPositionChangedEventArgs e)
    {
        // O VLC dispara em thread própria: marshall para a UI thread antes de tocar no binding.
        if (System.Windows.Application.Current is { } app && !app.Dispatcher.CheckAccess())
        {
            app.Dispatcher.BeginInvoke(() => OnPositionChanged(sender, e));
            return;
        }

        // O VLC pode reportar NaN enquanto ainda busca metadata/peers — ignora para não quebrar o slider.
        if (double.IsNaN(e.Position) || double.IsInfinity(e.Position)) return;

        Position = e.Position * 100.0;

        if (DuracaoTotalEmMilissegundos > 0)
        {
            long posicaoMs = (long)(e.Position * DuracaoTotalEmMilissegundos);
            PosicaoEmMilissegundos = posicaoMs;
        }
    }

    private void OnLengthChanged(object? sender, MediaPlayerLengthChangedEventArgs e)
    {
        long duracaoDoFilmeMs = e.Length;

        Utils.AtualizarUI(() =>
        {
            InicializarDuracaoDoVideo(duracaoDoFilmeMs);
        });
    }
}

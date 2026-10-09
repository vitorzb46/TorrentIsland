using LibVLCSharp.Shared;
using System.Collections.ObjectModel;
using TorrentIsland.Application.DTOs;
using TorrentIsland.Infrastructure.Logging;
using TorrentIsland.Presentation.Player.Common;

namespace TorrentIsland.Presentation.Player.ViewModel;

public partial class PlayerViewModel
{
    // BACKING FIELDS
    private Media? _media;
    private readonly Queue<Media> _playlist = new();
    private int _cliquesAvancar = 0;
    private int _cliquesRetroceder = 0;
    private DateTime _ultimoCliqueAvancar = DateTime.MinValue;
    private DateTime _ultimoCliqueRetroceder = DateTime.MinValue;

    // PROPERTIES
    public ObservableCollection<TrackItem> AudioTracks { get; } = [];

    public ObservableCollection<TrackItem> SubtitleTracks { get; } = [];

    private double AudioDelay { get; set; } = 0;

    private double SubtitleDelay { get; set; } = 0;

    public int CurrentAudio { get => _mediaPlayer.AudioTrack; }

    public int CurrentSpu { get => _mediaPlayer.Spu; }

    public long MediaTime { get; set; } = 0;

    public bool IsFullscreen { get; set { field = value; OnPropertyChanged(); } }

    public bool IsMuted { get; set { field = value; OnPropertyChanged(); } }

    public bool IsPlaying
    {
        get; set
        {
            if (field == value) return;
            field = value;
            OnPropertyChanged();
        }
    }

    public int Volume
    {
        get; set
        {
            var novo = Math.Clamp(value, 0, 100);
            if (novo == field) return;
            field = novo;
            _mediaPlayer.Volume = field;
            OnPropertyChanged();
        }
    } = 100;

    public double Position
    {
        get; set
        {
            if (Math.Abs(field - value) < 0.01) return;
            field = value;
            OnPropertyChanged();
        }
    }

    public long DuracaoTotalEmMilissegundos { get; set { field = value; OnPropertyChanged(); } }

    public long PosicaoEmMilissegundos { get; set { field = value; OnPropertyChanged(); TempoAtualFormatado = FormatarTempo(value); } }

    public string TempoAtualFormatado { get; set { field = value; OnPropertyChanged(); } } = "00:00:00";

    public string TempoTotalFormatado { get; set { field = value; OnPropertyChanged(); } } = "00:00:00";

    public string FeedbackTempo { get; private set { field = value; OnPropertyChanged(); } } = "";

    public bool MostrarFeedback { get; private set { field = value; OnPropertyChanged(); } } = false;

    // METHODS
    public void StopPlaybackState()
    {
        Stop();
        PosicaoEmMilissegundos = 0;
        DuracaoTotalEmMilissegundos = 0;
        TempoAtualFormatado = "00:00:00";
        TempoTotalFormatado = "00:00:00";
    }
    public void AddMedia(Media media) => _playlist.Enqueue(media);

    public async Task PlayNextMedia()
    {
        if (_playlist.Count == 0) return;
        var nextMedia = _playlist.Dequeue();
        StartPlayback(nextMedia);
        await LoadMediaTracksAsync();
    }

    public Media GetMediaPlayer() => _mediaPlayer.Media!;

    public Media? GetMedia() => _media;

    public void SetMedia(Media media, long time)
    {
        MediaDispose();
        StartPlayback(media);
        _mediaPlayer.SetPause(true);
        if (time > 5000)
        {
            SeekTo(TimeSpan.FromMilliseconds(time - 5000));
        }
    }

    public async Task SaveCacheAsync() =>
        await MediaTimestamp.SaveCache(FilePath, MediaTime);

    public Media? MediaDispose()
    {
        _media?.Dispose();
        _mediaPlayer.Media?.Dispose();
        _mediaPlayer.Media = null;
        return _media = null;
    }

    public void SetReset()
    {
        _mediaPlayer.SetPause(true);
        _mediaPlayer.SetPause(false);
    }

    public void Stop()
    {
        MediaDispose();
        _ = SaveCacheAsync();
        IsPlaying = false;
        IsVideoVisible = false;
        _mediaPlayer.Position = 0.0f;
        _mediaPlayer.Stop();
    }

    public void SetPause(bool pause) => _mediaPlayer.SetPause(pause);

    public void SeekTo(TimeSpan timeSpan)
    {
        if (DuracaoTotalEmMilissegundos > 0)
        {
            Position = (timeSpan.TotalMilliseconds / DuracaoTotalEmMilissegundos) * 100.0;
        }
        _mediaPlayer.SeekTo(timeSpan);
    }

    public void SetMute(bool mute)
    {
        _mediaPlayer.Mute = mute;
        IsMuted = mute;
    }

    public void ToggleVolume(bool volume)
    {
        if (volume)
        {
            if (Volume < 100)
            {
                Volume += 5;
            }
        }
        else
        {
            if (Volume > 0)
            {
                Volume -= 5;
            }
        }
    }

    public void ToggleDelayAudio(bool atrasar, double timeDelay = 0.5)
    {
        AudioDelay = AdjustDelay(AudioDelay, atrasar, timeDelay, "áudio", (valor) => _mediaPlayer.SetAudioDelay(valor));
    }

    public void ToggleDelaySpu(bool delay, double timeDelay = 0.5)
    {
        SubtitleDelay = AdjustDelay(SubtitleDelay, delay, timeDelay, "legenda", (valor) => _mediaPlayer.SetSpuDelay(valor));
    }

    private double AdjustDelay(double track, bool atrasar, double timeDelay, string trackName, Action<long> action)
    {
        if (atrasar)
        {
            track += timeDelay;
        }
        else
        {
            track -= timeDelay;
        }

        action((long)track * 1000000);
        OsdMessage = $"Ressincronizar {trackName}: {track:0.000;-0.000;0.000} seg.";
        return track;
    }

    public void InicializarDuracaoDoVideo(long totalMilliseconds)
    {
        DuracaoTotalEmMilissegundos = totalMilliseconds;
        TempoTotalFormatado = FormatarTempo(totalMilliseconds);
    }

    public void AvancarTempo()
    {
        var agora = DateTime.Now;

        if ((agora - _ultimoCliqueAvancar).TotalSeconds > 2)
        {
            _cliquesAvancar = 0;
        }

        _cliquesAvancar++;
        _ultimoCliqueAvancar = agora;

        int segundosParaAvancar = ObterSegundosProgressivos(_cliquesAvancar);

        var posicaoAtual = _mediaPlayer.Position;
        var duracaoTotal = _mediaPlayer.Length;

        if (duracaoTotal > 0)
        {
            var novaPosicao = Math.Min(1.0f, posicaoAtual + (segundosParaAvancar * 1000.0f / duracaoTotal));
            _mediaPlayer.Position = novaPosicao;

            PosicaoEmMilissegundos = (long)(novaPosicao * duracaoTotal);
            Position = novaPosicao * 100.0;

            MostrarFeedbackTempo($"⏩ +{segundosParaAvancar}s", segundosParaAvancar);
        }
    }

    public void RetrocederTempo()
    {
        var agora = DateTime.Now;

        if ((agora - _ultimoCliqueRetroceder).TotalSeconds > 2)
        {
            _cliquesRetroceder = 0;
        }

        _cliquesRetroceder++;
        _ultimoCliqueRetroceder = agora;

        int segundosParaRetroceder = ObterSegundosProgressivos(_cliquesRetroceder);

        var posicaoAtual = _mediaPlayer.Position;
        var duracaoTotal = _mediaPlayer.Length;

        if (duracaoTotal > 0)
        {
            var novaPosicao = Math.Max(0.0f, posicaoAtual - (segundosParaRetroceder * 1000.0f / duracaoTotal));
            _mediaPlayer.Position = novaPosicao;

            PosicaoEmMilissegundos = (long)(novaPosicao * duracaoTotal);
            Position = novaPosicao * 100.0;

            MostrarFeedbackTempo($"⏪ -{segundosParaRetroceder}s", -segundosParaRetroceder);
        }
    }

    private void StartPlayback(Media media)
    {
        _media = media;
        _mediaPlayer.Media = media;
        AudioDelay = 0;
        SubtitleDelay = 0;
        _mediaPlayer.Play(_media);
    }

    private async void MostrarFeedbackTempo(string texto, int segundos)
    {
        FeedbackTempo = texto;
        MostrarFeedback = true;

        await Task.Delay(1500);

        MostrarFeedback = false;
    }

    private static string FormatarTempo(long milissegundos)
    {
        TimeSpan tempo = TimeSpan.FromMilliseconds(milissegundos);

        if (tempo.TotalHours >= 1)
        {
            return tempo.ToString(@"hh\:mm\:ss");
        }
        return tempo.ToString(@"mm\:ss");
    }

    private int ObterSegundosProgressivos(int cliques)
    {
        return cliques switch
        {
            1 => 5,
            2 => 15,
            3 => 30,
            _ => 60 // 4 ou mais cliques
        };
    }

    private void OnPlaying(object? sender, EventArgs e)
    {
        IsPlaying = true;
        VlcHwnd = _mediaPlayer.Hwnd;

        _mediaPlayer.SetAdjustFloat(VideoAdjustOption.Enable, 1.0f);
        _mediaPlayer.SetAdjustFloat(VideoAdjustOption.Contrast, _contrast);
        _mediaPlayer.SetAdjustFloat(VideoAdjustOption.Brightness, _brightness);
        _mediaPlayer.SetAdjustFloat(VideoAdjustOption.Hue, _hue);
        _mediaPlayer.SetAdjustFloat(VideoAdjustOption.Saturation, _saturation);
        _mediaPlayer.SetAdjustFloat(VideoAdjustOption.Gamma, _gamma);
    }

    private void OnMute(object? sender, EventArgs e) => IsMuted = _mediaPlayer.Mute;

    private void OnPaused(object? sender, EventArgs e)
    {
        MediaTime = _mediaPlayer.Time;
        IsPlaying = false;
        if (IsOpening) return;
        IsLoading = false;
    }

    private void OnStopped(object? sender, EventArgs e)
    {
        IsPlaying = false;
        IsLoading = false;
    }
    private async void OnEndReached(object? sender, EventArgs e)
    {
        IsPlaying = false;
        IsLoading = false;
        try
        {
            await Task.Run(async () =>
            {
                MediaDispose();
                await PlayNextMedia();
            });
        }
        catch (Exception ex)
        {
            Log.Salvar($"Erro ao tentar reproduzir próximo item da playlist: {ex.Message} {ex.StackTrace}");
        }
    }
    private void OnEncounteredError(object? sender, EventArgs e)
    {
        Log.Salvar($"EncounteredError | State={_mediaPlayer.State} | Mrl={_mediaPlayer.Media?.Mrl}");

        if (_mediaPlayer.Media != null)
        {
            Log.Salvar($"Media Type: {_mediaPlayer.Media.Type}");
            Log.Salvar($"Media State: {_mediaPlayer.Media.State}");
            Log.Salvar($"Media Duration: {_mediaPlayer.Media.Duration}");
            Log.Salvar($"Media Tracks: {_mediaPlayer.Media.Tracks?.Length ?? 0}");
        }
        IsLoading = false;
        IsPlaying = false;
    }
}
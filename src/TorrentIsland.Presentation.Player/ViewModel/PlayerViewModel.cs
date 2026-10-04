using LibVLCSharp.Shared;
using LibVLCSharp.Shared.Structures;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Windows.Input;
using System.Windows.Threading;
using TorrentIsland.Application.DTOs;
using TorrentIsland.Application.Interfaces;
using TorrentIsland.Application.Medias.Enums;
using TorrentIsland.Application.Medias.Events;
using TorrentIsland.Infrastructure.Events;
using TorrentIsland.Infrastructure.Logging;
using TorrentIsland.Infrastructure.Subtitles;
using TorrentIsland.Presentation.Player.Common;

namespace TorrentIsland.Presentation.Player.ViewModel;

public sealed partial class PlayerViewModel : INotifyPropertyChanged, IDisposable
{
    #region Fields
    private readonly MediaPlayer _mediaPlayer;
    private Media? _media;
    private readonly Queue<Media> _playlist = new();
    private readonly IFormattingHelper _fb;
    private readonly DispatcherTimer _loadingDebounce;
    private bool _disposed;
    private int _cliquesAvancar = 0;
    private int _cliquesRetroceder = 0;
    private DateTime _ultimoCliqueAvancar = DateTime.MinValue;
    private DateTime _ultimoCliqueRetroceder = DateTime.MinValue;
    private const float ContrastMin = 0.0f, ContrastMax = 2.0f, ContrastDefault = 1.0f;
    private const float BrightnessMin = 0.0f, BrightnessMax = 2.0f, BrightnessDefault = 1.0f;
    private const int   HueMin = -180,  HueMax = 180,  HueDefault = 0;
    private const float SaturationMin = 0.0f, SaturationMax = 3.0f, SaturationDefault = 1.0f;
    private const float GammaMin = 0.01f, GammaMax = 10.0f, GammaDefault = 1.0f;

    private float _contrast = ContrastDefault;
    private float _brightness = BrightnessDefault;
    private int   _hue = HueDefault;
    private float _saturation = SaturationDefault;
    private float _gamma = GammaDefault;

    private ICommand? _openVideoCommand;
    private ICommand? _openAudioExternalCommand;
    private ICommand? _openSubtitleExternalCommand;
    private ICommand? _openTorrentFileCommand;
    private ICommand? _selectSubtitleCommand;
    private ICommand? _selectAudioCommand;
    private ICommand? _toggleFullScreenCommand;
    private ICommand? _togglePlayCommand;
    private ICommand? _toggleMuteCommand;
    private ICommand? _stopPlayerCommand;
    private ICommand? _torrentSearchCommand;
    private ICommand? _torrentQueueCommand;
    private ICommand? _delaySpuCommand;
    private ICommand? _setAspectRatioCommand;
    private ICommand? _setZoomCommand;
    private ICommand? _setContrastCommand;
    private ICommand? _setBrightnessCommand;
    private ICommand? _setHueCommand;
    private ICommand? _setSaturationCommand;
    private ICommand? _setGammaCommand;
    private ICommand? _resetImageCommand;
    private ICommand? _openFolderCommand;

    [GeneratedRegex(@".*?(?:s\d+e\d+|\d+x\d+)", RegexOptions.IgnoreCase)]
    private static partial Regex SeasonEpisode();
    #endregion

    #region Constructor
    public PlayerViewModel(LibVLC libVLC, MediaPlayer mediaPlayer, IFormattingHelper fb)
    {
        LibVLC = libVLC;
        _mediaPlayer = mediaPlayer;
        _mediaPlayer.SetAdjustFloat(VideoAdjustOption.Enable, 1.0f);
        _fb = fb;
        _mediaPlayer.PositionChanged += OnPositionChanged;
        _mediaPlayer.Playing += OnPlaying;
        _mediaPlayer.Paused += OnPaused;
        _mediaPlayer.Muted += OnMute;
        _mediaPlayer.Unmuted += OnMute;
        _mediaPlayer.Stopped += OnStopped;
        _mediaPlayer.EndReached += OnEndReached;
        _mediaPlayer.Buffering += OnPlayerBuffering;
        _mediaPlayer.MediaChanged += OnMediaChanged;
        _mediaPlayer.EncounteredError += OnEncounteredError;
        _mediaPlayer.LengthChanged += OnLengthChanged;
        Loading.LoadingMessageChanged += OnLoadingMessageChanged;
        TorrentStatusEvent.TorrentUpdated += OnTorrentUpdated;
        TorrentStatusEvent.TorrentQueue += OnTorrentQueue;

        _loadingDebounce = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(500)
        };
        _loadingDebounce.Tick += (_, _) =>
        {
            Log.Salvar("[Debounce] Tick disparou — fechando IsLoading");
            _loadingDebounce.Stop();
            IsLoading = false;
        };
    }
    #endregion

    #region Properties
    public ObservableCollection<TrackItem> AudioTracks { get; } = [];
    public ObservableCollection<TrackItem> SubtitleTracks { get; } = [];
    public ObservableCollection<TorrentDownloadDto> TorrentDownloads { get; } = [];

    #region Commands
    public ICommand OpenVideoCommand =>
        _openVideoCommand ??= new RelayCommand(
            () => OpenFileExternalRequested?.Invoke(this, new ExternalMediaEventArgs(FileType.Video)));
    public ICommand OpenAudioExternalCommand =>
        _openAudioExternalCommand ??= new RelayCommand(
            () => OpenFileExternalRequested?.Invoke(this, new ExternalMediaEventArgs(FileType.Audio)));
    public ICommand OpenSubtitleExternalCommand =>
        _openSubtitleExternalCommand ??= new RelayCommand(
            () => OpenFileExternalRequested?.Invoke(this, new ExternalMediaEventArgs(FileType.Subtitle)));    
    public ICommand OpenTorrentFileCommand =>
        _openTorrentFileCommand ??= new RelayCommand(
            () => OpenFileExternalRequested?.Invoke(this, new ExternalMediaEventArgs(FileType.Torrent)));    
    public ICommand OpenFolderCommand =>
        _openFolderCommand ??= new RelayCommand(
            () => OpenFileExternalRequested?.Invoke(this, new ExternalMediaEventArgs(FileType.Folder)));
    public ICommand ToggleFullscreenCommand =>
        _toggleFullScreenCommand ??= new RelayCommand(
            () => PlaybackActionRequested?.Invoke(this, new PlaybackEventArgs(PlaybackType.Fullscreen)));
    public ICommand StopPlayerCommand =>
        _stopPlayerCommand ??= new RelayCommand(
            () => PlaybackActionRequested?.Invoke(this, new PlaybackEventArgs(PlaybackType.Stop)));
    public ICommand TorrentSearchCommand =>
        _torrentSearchCommand ??= new RelayCommand(
            () => TorrentSearchRequested?.Invoke(this, EventArgs.Empty));
    public ICommand TorrentQueueCommand =>
        _torrentQueueCommand ??= new RelayCommand(
            () => TorrentQueueRequested?.Invoke(this, EventArgs.Empty));
    public ICommand SelectSubtitleCommand =>
        _selectSubtitleCommand ??= new RelayCommand<int>(SelectSubtitleTrack);
    public ICommand SelectAudioCommand =>
        _selectAudioCommand ??= new RelayCommand<int>(SelectAudioTrack);
    public ICommand DelaySpuCommand =>
        _delaySpuCommand ??= new RelayCommand<(bool delay, double timeDelay)>(
            args =>
            {
                ToggleDelaySpu(args.delay, args.timeDelay);
                SubtitleDelayChanged?.Invoke(this, EventArgs.Empty);
            });
    public ICommand TogglePlayCommand =>
        _togglePlayCommand ??= new RelayCommand(TogglePlay);
    public ICommand ToggleMuteCommand =>
        _toggleMuteCommand ??= new RelayCommand(ToggleMute);
    public ICommand SetAspectRatioCommand =>
        _setAspectRatioCommand ??= new RelayCommand<string>(SetAspectRatio);
    public ICommand SetZoomCommand =>
        _setZoomCommand ??= new RelayCommand<float>(SetZoom);
    public ICommand SetContrastCommand   => _setContrastCommand   
        ??= new RelayCommand<float>(value => Contrast = value);
    public ICommand SetBrightnessCommand => _setBrightnessCommand 
        ??= new RelayCommand<float>(value => Brightness = value);
    public ICommand SetHueCommand        => _setHueCommand        
        ??= new RelayCommand<float>(value => Hue = (int)value);
    public ICommand SetSaturationCommand => _setSaturationCommand 
        ??= new RelayCommand<float>(value => Saturation = value);
    public ICommand SetGammaCommand      => _setGammaCommand      
        ??= new RelayCommand<float>(value => Gamma = value);

    public ICommand ResetImageCommand =>
        _resetImageCommand ??= new RelayCommand(() =>
        {
            Contrast = ContrastDefault;
            Brightness = BrightnessDefault;
            Hue = HueDefault;
            Saturation = SaturationDefault;
            Gamma = GammaDefault;
        });
    #endregion

    public int CurrentAudio { get => _mediaPlayer.AudioTrack; }
    public int CurrentSpu { get => _mediaPlayer.Spu; }
    private static double SubtitleDelay { get; set; } = 0;
    public long MediaTime { get; set; } = 0;
    public static nint VlcHwnd { get; private set; }
    public LibVLC LibVLC { get; }
    public static string FileName =>
        Path.GetFileName(FilePath) ?? string.Empty;
    public bool IsOpening { get; set { field = value; OnPropertyChanged(); } } = false;

    public float Contrast
    {
        get => _contrast;
        private set
        {
            var estimate = Math.Clamp(value, ContrastMin, ContrastMax);
            if (Math.Abs(_contrast - estimate) < 0.001f) return;
            _contrast = estimate;
            _mediaPlayer.SetAdjustFloat(VideoAdjustOption.Contrast, estimate);
            OnPropertyChanged();
        }
    }

    public float Brightness
    {
        get => _brightness;
        private set
        {
            var estimate = Math.Clamp(value, BrightnessMin, BrightnessMax);
            if (Math.Abs(_brightness - estimate) < 0.001f) return;
            _brightness = estimate;
            _mediaPlayer.SetAdjustFloat(VideoAdjustOption.Brightness, estimate);
            OnPropertyChanged();
        }
    }

    public int Hue
    {
        get => _hue;
        private set
        {
            var estimate = Math.Clamp(value, HueMin, HueMax);
            if (_hue == estimate) return;
            _hue = estimate;
            _mediaPlayer.SetAdjustFloat(VideoAdjustOption.Hue, estimate);
            OnPropertyChanged();
        }
    }

    public float Saturation
    {
        get => _saturation;
        private set
        {
            var estimate = Math.Clamp(value, SaturationMin, SaturationMax);
            if (Math.Abs(_saturation - estimate) < 0.001f) return;
            _saturation = estimate;
            _mediaPlayer.SetAdjustFloat(VideoAdjustOption.Saturation, estimate);
            OnPropertyChanged();
        }
    }

    public float Gamma
    {
        get => _gamma;
        private set
        {
            var estimate = Math.Clamp(value, GammaMin, GammaMax);
            if (Math.Abs(_gamma - estimate) < 0.001f) return;
            _gamma = estimate;
            _mediaPlayer.SetAdjustFloat(VideoAdjustOption.Gamma, estimate);
            OnPropertyChanged();
        }
    }

    public string? CurrentAspectRatio
    {
        get;
        private set
        {
            if (field == value) return;
            field = value;
            OnPropertyChanged();
        }
    }

    public float CurrentZoom
    {
        get;
        private set
        {
            if (Math.Abs(field - value) < 0.001f) return;
            field = value;
            OnPropertyChanged();
        }
    }
    public static string FilePath
    {
        get;
        set
        {
            if (field == value) return;
            field = value;
            FilePathChanged?.Invoke();
        }
    } = string.Empty;

    public string TorrentName
    {
        get => field ?? "N/A";
        set
        {
            if (field != value)
            {
                field = value;
                OnPropertyChanged();
            }
        }
    }

    public double ProgressPercentage
    {
        get;
        set
        {
            if (field != value)
            {
                field = value;
                OnPropertyChanged();
            }
        }
    }

    public string Status
    {
        get => field ?? "N/A";
        set
        {
            if (field != value)
            {
                field = value;
                OnPropertyChanged();
            }
        }
    }

    [field: AllowNull, MaybeNull]
    public string? DownloadSpeed
    {
        get => field ?? "N/A";
        set
        {
            if (field != value)
            {
                field = value;
                OnPropertyChanged();
            }
        }
    }

    public string UploadSpeed
    {
        get => field ?? "N/A";
        set
        {
            if (field != value)
            {
                field = value;
                OnPropertyChanged();
            }
        }
    }

    public int Seeds
    {
        get;
        set
        {
            if (field != value)
            {
                field = value;
                OnPropertyChanged();
            }
        }
    }

    public int Peers
    {
        get;
        set
        {
            if (field != value)
            {
                field = value;
                OnPropertyChanged();
            }
        }
    }

    public string OsdMessage
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged();
        }
    } = string.Empty;

    public string LoadingMessage
    {
        get;
        set
        {
            if (field != value)
            {
                field = value;
                OnPropertyChanged();
            }
        }
    } = "Carregando...";

    public bool IsVideoVisible { get; set { field = value; OnPropertyChanged(); } } = false;

    public bool IsLoading
    {
        get; set
        {
            if (field == value) return;
            field = value;
            OnPropertyChanged();
        }
    }

    public bool IsFullscreen { get; set { field = value; OnPropertyChanged(); } }

    public bool IsPlaying
    {
        get; set
        {
            if (field == value) return;
            field = value;
            OnPropertyChanged();
        }
    }

    public bool IsMuted { get; set { field = value; OnPropertyChanged(); } }

    public double Position
    {
        get; set
        {
            if (Math.Abs(field - value) < 0.01) return;
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

    public long DuracaoTotalEmMilissegundos { get; set { field = value; OnPropertyChanged(); } }

    public long PosicaoEmMilissegundos { get; set { field = value; OnPropertyChanged(); TempoAtualFormatado = FormatarTempo(value); } }

    public string TempoAtualFormatado { get; set { field = value; OnPropertyChanged(); } } = "00:00:00";

    public string TempoTotalFormatado { get; set { field = value; OnPropertyChanged(); } } = "00:00:00";

    public string FeedbackTempo { get; private set { field = value; OnPropertyChanged(); } } = "";

    public bool MostrarFeedback { get; private set { field = value; OnPropertyChanged(); } } = false;
    #endregion

    #region Public Methods
    public void AddMedia(Media media)
    {
        _playlist.Enqueue(media);
        Task.Run(async () => await ProcessarLegendasUndAsync(media));
    }

    public void PlayNextMedia()
    {
        if (_playlist.Count == 0) return;
        var nextMedia = _playlist.Dequeue();
        StartPlayback(nextMedia);
    }
    public void IncrementBrightness(float step = 0.05f) => Brightness = _brightness + step;

    public void DecrementBrightness(float step = 0.05f) => Brightness = _brightness - step;

    public void IncrementContrast(float step = 0.05f) => Contrast = _contrast + step;

    public void DecrementContrast(float step = 0.05f) => Contrast = _contrast - step;

    public void IncrementHue(int step = 5) => Hue = _hue + step;

    public void DecrementHue(int step = 5) => Hue = _hue - step;

    public void IncrementSaturation(float step = 0.1f) => Saturation = _saturation + step;

    public void DecrementSaturation(float step = 0.1f) => Saturation = _saturation - step;

    public void IncrementGamma(float step = 0.1f) => Gamma = _gamma + step;

    public void DecrementGamma(float step = 0.1f) => Gamma = _gamma - step;

    public async Task SaveCacheAsync()
    {
        await MediaTimestamp.SaveCache(FilePath, MediaTime);
    }

    public void SelectAudioTrack(int trackId) => _mediaPlayer.SetAudioTrack(trackId);

    public void SelectSubtitleTrack(int spuId) => _mediaPlayer.SetSpu(spuId);

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
        _mediaPlayer.Stop();
    }

    public void SetPause(bool pause) => _mediaPlayer.SetPause(pause);

    public void InicializarDuracaoDoVideo(long totalMilliseconds)
    {
        DuracaoTotalEmMilissegundos = totalMilliseconds;
        TempoTotalFormatado = FormatarTempo(totalMilliseconds);
    }

    public Media GetMediaPlayer() => _mediaPlayer.Media!;

    public Media? GetMedia() => _media;

    public Media? MediaDispose()
    {
        _media?.Dispose();
        _mediaPlayer.Media?.Dispose();
        _mediaPlayer.Media = null;
        return _media = null;
    }

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

    public void ToggleMute()
    {
        _mediaPlayer.Mute = !_mediaPlayer.Mute;
        IsMuted = _mediaPlayer.Mute;
    }

    public void TogglePlay()
    {
        if (_mediaPlayer.IsPlaying)
            _mediaPlayer.Pause();
        else
            _mediaPlayer.Play();
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

    private void ZeroingDelaySpu(bool delay) => ToggleDelaySpu(delay, 0);

    private void DelaySpu5s(bool delay) => ToggleDelaySpu(delay, 5);

    public void ToggleDelaySpu(bool delay, double timeDelay = 0.5)
    {
        if (delay)
        {
            SubtitleDelay += timeDelay;
        }
        else
        {
            SubtitleDelay -= timeDelay;
        }
        
        _mediaPlayer.SetSpuDelay((long)SubtitleDelay * 1000000);
        OsdMessage = $"Ressincronizar legenda: {SubtitleDelay:0.000;-0.000;0.000} seg.";
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

    public async Task PopulateTracksAsync(CancellationToken ct = default)
    {
        var esperaInicio = Task.Delay(TimeSpan.FromSeconds(5), ct);
        while (!_mediaPlayer.IsPlaying && _mediaPlayer.State != VLCState.Ended && _mediaPlayer.State != VLCState.Error)
        {
            ct.ThrowIfCancellationRequested();
            if (await Task.WhenAny(Task.Delay(1000, ct), esperaInicio).ConfigureAwait(true) == esperaInicio)
            {
                break;
            }
        }

        if (System.Windows.Application.Current is { } app && !app.Dispatcher.CheckAccess())
        {
            await app.Dispatcher.InvokeAsync(() => PopulateTracksAsync(ct)).Task.ConfigureAwait(true);
            return;
        }

        await Task.Run(async () =>
        {
            await TimeLogging.Time(async () =>
            {
                await ProcessarLegendasUndAsync(_media).ConfigureAwait(false);
            }, "ProcessarLegendasUndAsync");
        }, ct);

        //await ProcessarLegendasUndAsync().ConfigureAwait(false);
    }

    public void LoadExternalAudio(string filePath)
    {
        if (filePath is not string path || !File.Exists(path)) return;
        ParseFileNameInfo(
            path,
            out _,
            out int audioId,
            out string uri,
            out string fileName);
        AudioTracks.Add(new TrackItem(audioId, fileName));
        _mediaPlayer.AddSlave(MediaSlaveType.Audio, uri, select: true);
    }

    public void LoadExternalSubtitle(object? filePath)
    {
        if (filePath is not string path || !File.Exists(path)) return;
        ParseFileNameInfo(
            path,
            out int subId,
            out _,
            out string uri,
            out string fileName);
        SubtitleTracks.Add(new TrackItem(subId, fileName));
        _mediaPlayer.AddSlave(MediaSlaveType.Subtitle, uri, select: true);
    }

    private void ParseFileNameInfo(string path, out int subId, out int audioId, out string uri, out string fileName)
    {
        subId = SubtitleTracks.Count + 90;
        audioId = AudioTracks.Count + 90;
        uri = new Uri(path).AbsoluteUri;
        fileName = Path.GetFileNameWithoutExtension(path);
        var match = SeasonEpisode().Match(fileName);

        if (match.Success)
        {
            fileName = string.Concat(match.Value, "...");
        }
        else if (fileName.Length > 20)
        {
            fileName = string.Concat(fileName.AsSpan(0, 20), "...");
        }
    }
    #endregion

    #region Private Methods  
    private void StartPlayback(Media media)
    {
        _media = media;
        _mediaPlayer.Media = media;
        _mediaPlayer.Play(_media);
        IsVideoVisible = true;
    }  
    private void SetAspectRatio(string? ratio)
    {
        CurrentAspectRatio = ratio;
        _mediaPlayer.AspectRatio = ratio;
    }

    private void SetZoom(float scale)
    {
        CurrentZoom = scale;
        _mediaPlayer.Scale = scale;
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

    // Método para mostrar feedback visual
    private async void MostrarFeedbackTempo(string texto, int segundos)
    {
        FeedbackTempo = texto;
        MostrarFeedback = true;

        // Aguarda 1.5 segundos e esconde o feedback
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

    private async Task ProcessarLegendasUndAsync(Media? media)
    {
        try
        {
            if (string.IsNullOrEmpty(FilePath) || !File.Exists(FilePath))
            {
                Log.Salvar("Caminho do vídeo inválido ou arquivo não encontrado.");
                return;
            }
            
            // media = _mediaPlayer.Media;

            if (media == null) return;
            TrackDescription[]? audioTracks = _mediaPlayer.AudioTrackDescription;
            TrackDescription[]? spuTracks = _mediaPlayer.SpuDescription;

            if (audioTracks is not { Length: > 0 }) return;
            if (spuTracks is not { Length: > 0 }) return;

            string idiomaUsuario = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
            string caminhoTempBase = Path.Combine(Path.GetTempPath(), ".SubExtract");

            ConcurrentBag<TrackItem> novosTracks = [];
            Dictionary<int, (string? Language, string? Description, uint Codec)>? metadadosLegendas;
            Dictionary<int, ulong> tracksParaDetectar = [];
            List<TrackDescription> undTracks = [];
            List<int> vlcIdsPorPosicao = [];

            metadadosLegendas = media.Tracks
                .Where(m => m.TrackType == TrackType.Text)
                .ToDictionary(t => t.Id, t => (t.Language, t.Description, t.Codec));

            undTracks = [.. spuTracks.Where(t => metadadosLegendas.ContainsKey(t.Id) &&
                                                 metadadosLegendas[t.Id].Language == "und")
                                                                  .OrderBy(t => t.Id)];

            if (undTracks.Count == 0) return;

            LoadingMessage = "Processando legendas...";

            // Limpa o diretório temporário se houver resquícios não tratados.
            if (Directory.Exists(caminhoTempBase))
                Directory.Delete(caminhoTempBase, true);

            Directory.CreateDirectory(caminhoTempBase);

            AudioTracks.Clear();
            AudioTracks.Add(new TrackItem(-1, "Desativar áudio"));
            foreach (var t in audioTracks.Where(t => t.Id >= 0))
            {
                AudioTracks.Add(new TrackItem(t.Id, NomeDaFaixa(t.Name, "Áudio", t.Id)));
            }

            SubtitleTracks.Clear();
            SubtitleTracks.Add(new TrackItem(-99, "Aguardando legendas..."));

            var mkvMetaOrdenada = Subtitle.GetMetadata(FilePath);
            if (undTracks.Count != mkvMetaOrdenada.Count)
            {
                Log.Salvar($"[Aviso] undTracks={undTracks.Count} " +
                        $"mkvTracks={mkvMetaOrdenada.Count} — mapeamento pode estar errado");
            }

            var n = Math.Min(undTracks.Count, mkvMetaOrdenada.Count);
            var semCacheMkv = new HashSet<ulong>();
            var mkvParaVlc = new Dictionary<ulong, int>();

            for (int i = 0; i < n; i++)
            {
                var vlcId = undTracks[i].Id;
                var mkvNum = mkvMetaOrdenada[i].TrackNumber;
                var cacheEntry = await Subtitle.TryGetAsync(FilePath, vlcId);

                if (cacheEntry != null)
                {
                    novosTracks.Add(new TrackItem(vlcId, cacheEntry.Language));
                    continue;
                }

                semCacheMkv.Add(mkvNum);
                mkvParaVlc[mkvNum] = vlcId;
                Log.Salvar($"Track sem cache: {mkvNum}");
            }

            if (semCacheMkv.Count > 0)
            {
                const int maxChars = 2000;

                var lista = Subtitle.Extraction(FilePath, semCacheMkv);

                // Correlaciona VLC Id ↔ SubtitleTrack por índice
                var trackTexts = new Dictionary<int, string>(mkvParaVlc.Count);

                foreach (var track in lista)
                {
                    if (!mkvParaVlc.TryGetValue(track.TrackNumber, out var vlcId))
                        continue;

                    var text = string.Join("\n", track.Cues
                        .Where(c => !string.IsNullOrEmpty(c.Text))
                        .Select(c => c.Text));

                    if (text.Length > maxChars)
                        text = text[..maxChars];

                    trackTexts[vlcId] = text;
                }

                await Subtitle.Detection(novosTracks, trackTexts, FilePath).ConfigureAwait(false);
            }

            /// Atualiza a coleção na UI
            await Utils.AtualizarUIAsync(async () =>
            {
                SubtitleTracks.Clear();
                SubtitleTracks.Add(new TrackItem(-1, "Desativar legenda"));
                var allSubs = novosTracks.ToDictionary(kvp => kvp.Id, kvp => kvp.Name)
                                         .OrderBy(i => !i.Value.Contains(idiomaUsuario, StringComparison.CurrentCultureIgnoreCase))
                                         .ThenBy(i => i.Value, StringComparer.Create(CultureInfo.CurrentCulture, ignoreCase: true))
                                         .ToList();

                foreach (var item in allSubs)
                {
                    SubtitleTracks.Add(new TrackItem(item.Key, NomeDaFaixa(null, "Legenda", item.Key, item.Value)));
                }
            });
        }
        catch (Exception ex)
        {
            Log.Salvar($"Erro em ProcessarLegendasUndAsync: {ex.Message} {ex.StackTrace}");
        }
    }

    /// <summary>
    /// Gera um nome legível para uma faixa: prioriza o idioma/descrição reais dos metadados
    /// (Media.Tracks), mapeia códigos de idioma (ex.: "por" → "Português"),
    /// converte "Track N" genérico em "Áudio N"/"Legenda N" e preserva descrições reais.
    /// </summary>
    private static string NomeDaFaixa(string? nome, string tipo, int id, string? idiomaReal = null, string? descricaoReal = null)
    {
        // "und"/"undetermined" = idioma indefinido → trata como ausente.
        var idioma = Utils.EhIdiomaValido(idiomaReal) ? Utils.TraduzirIdioma(idiomaReal!) : null;

        if (idioma is not null)
        {
            if (!string.IsNullOrWhiteSpace(descricaoReal))
            {
                Log.Salvar($"Nome da faixa: {idioma} - {descricaoReal}{Environment.NewLine}");
                return $"{idioma} - {descricaoReal}";
            }
            return idioma;
        }
        else
        {
            Log.Salvar($"Idioma não classificado: {idiomaReal}{Environment.NewLine}");
        }

        if (!string.IsNullOrWhiteSpace(descricaoReal))
        {
            return descricaoReal;
        }

        if (string.IsNullOrWhiteSpace(nome) || Utils.EhIdiomaIndefinido(nome))
        {
            return $"{tipo} {id} (Desconhecido)";
        }

        var limpo = nome.Trim().ToLower();
        string? idiomaDetectado = Utils.TraduzirIdioma(nome.Trim());

        // Se o VLC retornar apenas "Track N", padroniza o termo
        if (limpo.StartsWith("track", StringComparison.OrdinalIgnoreCase))
        {
            return $"{tipo} {id}";
        }

        return idiomaDetectado is null ? $"{tipo} {id}" : $"{idiomaDetectado} [{id}]";
    }
    #endregion

    #region Events Handlers
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

    private void OnPropertyChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
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
    private void OnEndReached(object? sender, EventArgs e)
    {
        IsPlaying = false;
        IsLoading = false;        
        try
        {
            ThreadPool.QueueUserWorkItem(_ =>
            {
                MediaDispose();
                PlayNextMedia();
            });
        }
        catch (Exception ex)
        {
            Log.Salvar($"Erro ao tentar reproduzir próximo item da playlist: {ex.Message} {ex.StackTrace}");
        }
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
    private void OnMediaChanged(object? sender, MediaPlayerMediaChangedEventArgs e)
    {
        // IsLoading = true;
        Utils.AtualizarUI(() =>
        {
            AudioTracks.Clear();
            SubtitleTracks.Clear();
        });        
        VlcHwnd = _mediaPlayer.Hwnd;
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
    private void OnLengthChanged(object? sender, MediaPlayerLengthChangedEventArgs e)
    {
        long duracaoDoFilmeMs = e.Length;

        Utils.AtualizarUI(() =>
        {
            InicializarDuracaoDoVideo(duracaoDoFilmeMs);
        });
    }
    private void OnLoadingMessageChanged(object? sender, string e)
    {
        Utils.AtualizarUI(() =>
        {
            LoadingMessage = e;
        });
    }
    private void OnTorrentUpdated(object? sender, TorrentDto dto)
    {
        Utils.AtualizarUI(() =>
        {
            TorrentName = dto.Nome ?? "Desconhecido";
            ProgressPercentage = _fb.FormatarPorcentagem(dto.Progresso);
            Status = dto.Estado.ToString();
            DownloadSpeed = _fb.FormatarBytes(dto.VelocidadeDownload);
            UploadSpeed = _fb.FormatarBytes(dto.VelocidadeUpload);
            Seeds = dto.Seeds;
            Peers = dto.ParesDisponiveis;
        });
    }
    private void OnTorrentQueue(object? sender, TorrentDownloadDto dto)
    {
        Utils.AtualizarUI(() =>
        {

            var torrent = TorrentDownloads.FirstOrDefault(t => t.TorrentId == dto.TorrentId);
            if (torrent == null)
            {
                TorrentDownloads.Add(dto);
            }
            else
            {
                torrent!.TorrentName = dto.TorrentName;
                torrent.Status = dto.Status;
                torrent.Progress = dto.Progress;
                torrent.DownloadSpeed = dto.DownloadSpeed;
                torrent.UploadSpeed = dto.UploadSpeed;
                torrent.Seeds = dto.Seeds;
                torrent.Peers = dto.Peers;
                torrent.TimeRemaining = dto.TimeRemaining;
            }
        });
    }
    #endregion


    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _mediaPlayer.PositionChanged -= OnPositionChanged;
        _mediaPlayer.Playing -= OnPlaying;
        _mediaPlayer.Paused -= OnPaused;
        _mediaPlayer.Muted -= OnMute;
        _mediaPlayer.Unmuted -= OnMute;
        _mediaPlayer.Stopped -= OnStopped;
        _mediaPlayer.EndReached -= OnEndReached;
        _mediaPlayer.Buffering -= OnPlayerBuffering;
        _mediaPlayer.MediaChanged -= OnMediaChanged;
        _mediaPlayer.EncounteredError -= OnEncounteredError;
        _mediaPlayer.LengthChanged -= OnLengthChanged;
        Loading.LoadingMessageChanged -= OnLoadingMessageChanged;
        TorrentStatusEvent.TorrentUpdated -= OnTorrentUpdated;
        TorrentStatusEvent.TorrentQueue -= OnTorrentQueue;

        try
        {
            if (_mediaPlayer.Media != null)
            {
                MediaTime = _mediaPlayer.Time;
                _ = MediaTimestamp.SaveCache(FilePath, MediaTime);
            }
        }
        catch (Exception ex)
        {
            Log.Salvar($"Não foi possível salvar o tempo do MediaTime: {ex.Message}");
        }
    }
}
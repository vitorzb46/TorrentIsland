using LibVLCSharp.Shared;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using TorrentIsland.Application.DTOs;

namespace TorrentIsland.Presentation.Player;

public sealed class PlayerViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly LibVLC _libVLC;
    private readonly MediaPlayer _mediaPlayer;
    private Media? _media;
    private bool _disposed;
    private bool _isLoading;
    private bool _isFullscreen;
    private bool _isPlaying;
    private bool _isMuted;
    private double _position;
    private int _volume = 100;
    private long _duracaoTotalMs;
    private long _posicaoMs;
    public ObservableCollection<TrackItem> AudioTracks { get; } = [];
    public ObservableCollection<TrackItem> SubtitleTracks { get; } = [];
    private ICommand TogglePlayCommand { get; }
    private ICommand ToggleFullscreenCommand { get; }
    private ICommand LoadExternalSubtitleCommand { get; }
    private string _tempoAtualFormatado = "00:00:00";
    private string _tempoTotalFormatado = "00:00:00";
    private int _cliquesAvancar = 0;
    private int _cliquesRetroceder = 0;
    private DateTime _ultimoCliqueAvancar = DateTime.MinValue;
    private DateTime _ultimoCliqueRetroceder = DateTime.MinValue;
    private string _feedbackTempo = "";
    private bool _mostrarFeedback = false;
    public LibVLC LibVLC => _libVLC;

    public PlayerViewModel(LibVLC libVLC, MediaPlayer mediaPlayer)
    {
        Log.Salvar("PlayerViewModel iniciado");
        _libVLC = libVLC;
        _mediaPlayer = mediaPlayer;
        _mediaPlayer.PositionChanged += OnPositionChanged;
        _mediaPlayer.Playing += OnPlaying;
        _mediaPlayer.Paused += OnPaused;
        _mediaPlayer.Muted += OnMute;
        _mediaPlayer.Unmuted += OnMute;
        _mediaPlayer.Stopped += OnStopped;
        _mediaPlayer.EndReached += OnEndReached;
        _mediaPlayer.Buffering += OnPlayerBuffering;
        _mediaPlayer.EncounteredError += (_, _) =>
        {
            Log.Salvar($"EncounteredError | State={_mediaPlayer.State} | Mrl={_mediaPlayer.Media?.Mrl}");
            // Diagnóstico visível: escreve o erro do VLC em um arquivo de log local.
            try
            {
                File.AppendAllText(
                    Path.Combine(AppContext.BaseDirectory, "vlc-errors.log"),
                    $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] EncounteredError | State={_mediaPlayer.State} | Mrl={_mediaPlayer.Media?.Mrl}{Environment.NewLine}");
            }
            catch { /* log é best-effort */ }
        };

        _mediaPlayer.LengthChanged += (sender, args) =>
        {
            long duracaoDoFilmeMs = args.Length;

            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                InicializarDuracaoDoVideo(duracaoDoFilmeMs);
            });
        };

        TogglePlayCommand = new RelayCommand(TogglePlay);
        ToggleFullscreenCommand = new RelayCommand(ToggleFullscreen);
        LoadExternalSubtitleCommand = new RelayCommand(LoadExternalSubtitle);
    }

    public bool IsLoading
    {
        get => _isLoading;
        set { _isLoading = value; OnPropertyChanged(); }
    }

    public bool IsFullscreen
    {
        get => _isFullscreen;
        set { _isFullscreen = value; OnPropertyChanged(); }
    }

    public bool IsPlaying
    {
        get => _isPlaying;
        set { _isPlaying = value; OnPropertyChanged(); }
    }

    public bool IsMuted
    {
        get => _isMuted;
        set { _isMuted = value; OnPropertyChanged(); }
    }

    public double Position
    {
        get => _position;
        set
        {
            if (Math.Abs(_position - value) < 0.01) return;
            _position = value;
            OnPropertyChanged();
        }
    }

    public int Volume
    {
        get => _volume;
        set
        {
            var novo = Math.Clamp(value, 0, 100);
            if (novo == _volume) return;
            _volume = novo;
            _mediaPlayer.Volume = _volume;
            OnPropertyChanged();
        }
    }

    public long DuracaoTotalEmMilissegundos
    {
        get => _duracaoTotalMs;
        set { _duracaoTotalMs = value; OnPropertyChanged(); }
    }

    public long PosicaoEmMilissegundos
    {
        get => _posicaoMs;
        set { _posicaoMs = value; OnPropertyChanged(); }
    }

    public string TempoAtualFormatado
    {
        get => _tempoAtualFormatado;
        private set { _tempoAtualFormatado = value; OnPropertyChanged(); }
    }

    public string TempoTotalFormatado
    {
        get => _tempoTotalFormatado;
        private set { _tempoTotalFormatado = value; OnPropertyChanged(); }
    }
    
    public string FeedbackTempo
    {
        get => _feedbackTempo;
        private set { _feedbackTempo = value; OnPropertyChanged(); }
    }

    public bool MostrarFeedback
    {
        get => _mostrarFeedback;
        private set { _mostrarFeedback = value; OnPropertyChanged(); }
    }

    public void InicializarDuracaoDoVideo(long totalMilliseconds)
    {
        DuracaoTotalEmMilissegundos = totalMilliseconds;
        TempoTotalFormatado = FormatarTempo(totalMilliseconds);
    }

    public void SetMedia(Media media)
    {
        _media?.Dispose();
        _media = media;
        Log.Salvar($"SetMedia | Mrl={media.Mrl}");
        _mediaPlayer.Play(_media);
        Log.Salvar($"Play() chamado | State={_mediaPlayer.State}");
        IsLoading = true;
    }
    public void SeekTo2(TimeSpan timeSpan) => _mediaPlayer.SeekTo(timeSpan);
    public void SeekTo(double percent)
    {
        Log.Salvar($"SeekTo | percent={percent:0.0}");
        _mediaPlayer.Position = (float)Math.Clamp(percent / 100.0, 0.0, 1.0);
    }

    public void SetMute(bool mute)
    {
        Log.Salvar($"SetMute | mute={mute}");
        _mediaPlayer.Mute = mute;
        IsMuted = mute;
    }

    public void ToggleMute()
    {
        // _mediaPlayer.ToggleMute();
        IsMuted = _mediaPlayer.Mute;
        Log.Salvar($"ToggleMute | IsMuted={IsMuted}");
        if (IsMuted)
        {
            _mediaPlayer.Mute = false;
            Log.Salvar("MuteButton desativado");
        }
        else
        {
            _mediaPlayer.Mute = true;
            Log.Salvar("MuteButton ativado");
        }
    }

    public void TogglePlay()
    {
        Log.Salvar($"TogglePlay | IsPlaying={_mediaPlayer.IsPlaying} State={_mediaPlayer.State}");
        if (_mediaPlayer.IsPlaying)
        {
            _mediaPlayer.Pause();
        }
        else
        {
            _mediaPlayer.Play();
        }
    }

    public void ToggleFullscreen()
    {
        IsFullscreen = !IsFullscreen;
    }

    public void SelectAudioTrack(int trackId)
    {
        _mediaPlayer.SetAudioTrack(trackId);
    }

    public void SelectSubtitleTrack(int spuId)
    {
        try
        {
            _mediaPlayer.SetSpu(spuId);
        }
        catch (Exception ex)
        {
            Log.Salvar($"Erro ao injetar trilha de legenda no LibVLC: {ex.Message}");
        }
    }

    public void LoadExternalSubtitle(object? filePath)
    {
        if (filePath is not string path || !File.Exists(path)) return;

        _mediaPlayer.AddSlave(MediaSlaveType.Subtitle, path, select: true);

        //if (_mediaPlayer.AddSlave(MediaSlaveType.Subtitle, path, select: true))
        //{
        //    SubtitleTracks.Add(new TrackItem(-99, Path.GetFileName(path)));
        //    OnPropertyChanged(nameof(SubtitleTracks));
        //}
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
            TempoAtualFormatado = FormatarTempo(PosicaoEmMilissegundos);
            
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
            TempoAtualFormatado = FormatarTempo(PosicaoEmMilissegundos);
            
            MostrarFeedbackTempo($"⏪ -{segundosParaRetroceder}s", -segundosParaRetroceder);
        }
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

    public async Task PopulateTracksAsync(CancellationToken ct = default)
    {
        var audioTrackCount = _mediaPlayer.AudioTrackCount;
        var subtitleTrackCount = _mediaPlayer.SpuCount;
        var trackDesc = _mediaPlayer.SpuDescription ?? [];
        Log.Salvar($"Àudios: {audioTrackCount} | Legendas {subtitleTrackCount}");

        foreach (var t in trackDesc)
        {
            Log.Salvar($"Track: {t.Id} | {t.Name}");
        }

        // Aguarda o vídeo iniciar (o MediaPlayer precisa do media carregado), com timeout
        // para não travar a UI caso a mídia falhe (ex.: URL inacessível).
        var esperaInicio = Task.Delay(TimeSpan.FromSeconds(15), ct);
        while (!_mediaPlayer.IsPlaying && _mediaPlayer.State != VLCState.Ended && _mediaPlayer.State != VLCState.Error)
        {
            ct.ThrowIfCancellationRequested();
            if (await Task.WhenAny(Task.Delay(100, ct), esperaInicio).ConfigureAwait(true) == esperaInicio)
            {
                break; // timeout: segue para popular faixas mesmo se não iniciou
            }
        }

        // Garante que a mutação das ObservableCollection ocorra na UI thread (Dispatcher).
        if (System.Windows.Application.Current is { } app && !app.Dispatcher.CheckAccess())
        {
            await app.Dispatcher.InvokeAsync(() => PopulateTracksAsync(ct)).Task.ConfigureAwait(true);
            return;
        }

        var audioTracks = _mediaPlayer.AudioTrackDescription ?? [];
        AudioTracks.Clear();
        foreach (var t in audioTracks.Where(t => t.Id >= 0))
        {
            Log.Salvar($"AudioTrack: {t.Name}");
            AudioTracks.Add(new TrackItem(t.Id, NomeDaFaixa(t.Name, "Áudio", t.Id)));
        }

        var spuTracks = _mediaPlayer.SpuDescription;
        SubtitleTracks.Clear();

        var metadadosLegendas = _mediaPlayer.Media?.Tracks.Where(m => m.TrackType == TrackType.Text)
                                                          .ToDictionary(t => t.Id, t => (t.Language, t.Description));

        Log.Salvar($"Número de legendas: {spuTracks!.Length}");
        foreach (var legenda in spuTracks!)
        {
            Log.Salvar($"LEGENDAS: {legenda.Name} | {legenda.Id}");
            SubtitleTracks.Add(new TrackItem(legenda.Id, NomeDaFaixa(
                legenda.Name,
                "Legenda",
                legenda.Id,
                metadadosLegendas!.TryGetValue(legenda.Id, out var meta) ? meta.Language : null,
                metadadosLegendas!.TryGetValue(legenda.Id, out meta) ? meta.Description : null)));
        }

        IsLoading = false;
    }

    private static readonly Dictionary<string, string> Idiomas = new(StringComparer.OrdinalIgnoreCase)
    {
        ["pt"] = "Português",
        ["por"] = "Português",
        ["pt-br"] = "Português (BR)",
        ["en"] = "Inglês",
        ["eng"] = "Inglês",
        ["es"] = "Espanhol",
        ["spa"] = "Espanhol",
        ["fr"] = "Francês",
        ["fre"] = "Francês",
        ["fra"] = "Francês",
        ["de"] = "Alemão",
        ["ger"] = "Alemão",
        ["deu"] = "Alemão",
        ["it"] = "Italiano",
        ["ita"] = "Italiano",
        ["ja"] = "Japonês",
        ["jpn"] = "Japonês",
        ["ko"] = "Coreano",
        ["kor"] = "Coreano",
        ["zh"] = "Chinês",
        ["zho"] = "Chinês",
        ["ru"] = "Russo",
        ["rus"] = "Russo",
        ["ar"] = "Árabe",
        ["ara"] = "Árabe",
        ["hi"] = "Hindi",
        ["nl"] = "Holandês",
        ["nld"] = "Holandês",
        ["sv"] = "Sueco",
        ["swe"] = "Sueco",
        ["pl"] = "Polonês",
        ["pol"] = "Polonês",
    };

    /// <summary>
    /// Gera um nome legível para uma faixa: prioriza o idioma/descrição reais dos metadados
    /// (Media.Tracks), mapeia códigos de idioma (ex.: "por" → "Português"),
    /// converte "Track N" genérico em "Áudio N"/"Legenda N" e preserva descrições reais (ex.: "AC-3").
    /// </summary>
    private static string NomeDaFaixa(string? nome, string tipo, int id, string? idiomaReal = null, string? descricaoReal = null)
    {
        // 1. Idiomas/descrições reais dos metadados têm prioridade máxima.
        // "und"/"undetermined" = idioma indefinido → trata como ausente.
        var idioma = EhIdiomaValido(idiomaReal) ? TraduzirIdioma(idiomaReal!) : null;
        if (idioma is not null)
        {
            if (!string.IsNullOrWhiteSpace(descricaoReal))
            {
                return $"{idioma} - {descricaoReal}";
            }
            return idioma;
        }

        if (!string.IsNullOrWhiteSpace(descricaoReal))
        {
            return descricaoReal;
        }

        // 2. Sem metadados: usa o nome do SpuDescription.
        if (string.IsNullOrWhiteSpace(nome) || EhIdiomaIndefinido(nome))
        {
            return $"{tipo} {id}";
        }

        var limpo = nome.Trim().ToLower();
        string? idiomaDetectado = TraduzirIdioma(nome.Trim());

        // Se o VLC retornar apenas "Track N", padroniza o termo
        if (limpo.StartsWith("track", StringComparison.OrdinalIgnoreCase))
        {
            return $"{tipo} {id}";
        }

        // Retorna o nome do idioma acompanhado do número da faixa para o usuário conseguir diferenciar
        return idiomaDetectado is null ? $"{tipo} {id}" : $"{idiomaDetectado} [{id}]";
    }

    /// <summary>Indica se o código de idioma é realmente um idioma (não "und"/"undetermined"/vazio).</summary>
    private static bool EhIdiomaValido(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor)) return false;
        var limpo = valor.Trim().ToLower();
        return limpo is not ("und" or "undetermined" or "unknown" or "mis" or "mul" or "zxx" or "???");
    }

    /// <summary>Indica se o valor representa "idioma indefinido" (ex.: "und").</summary>
    private static bool EhIdiomaIndefinido(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor)) return true;
        return !EhIdiomaValido(valor);
    }

    private static string? TraduzirIdioma(string valor)
    {
        if (!EhIdiomaValido(valor)) return null;

        var limpo = valor.Trim().ToLower();
        if (limpo.Contains("por") || limpo.Contains("pt") || limpo.Contains("portuguese"))
        {
            return "Português (BR)";
        }
        if (limpo.Contains("eng") || limpo.Contains("en") || limpo.Contains("english"))
        {
            return "Inglês";
        }
        if (limpo.Contains("spa") || limpo.Contains("es") || limpo.Contains("spanish") || limpo.Contains("espanol"))
        {
            return "Espanhol";
        }
        if (limpo.Contains("fre") || limpo.Contains("fr") || limpo.Contains("french"))
        {
            return "Francês";
        }
        if (limpo.Contains("ger") || limpo.Contains("de") || limpo.Contains("german"))
        {
            return "Alemão";
        }
        if (limpo.Contains("jap") || limpo.Contains("ja") || limpo.Contains("japanese"))
        {
            return "Japonês";
        }
        if (limpo.Contains("kor") || limpo.Contains("ko") || limpo.Contains("korean"))
        {
            return "Coreano";
        }
        if (limpo.Contains("chi") || limpo.Contains("zh") || limpo.Contains("chinese"))
        {
            return "Chinês";
        }
        if (limpo.Contains("rus") || limpo.Contains("ru") || limpo.Contains("russian"))
        {
            return "Russo";
        }
        if (limpo.Contains("ara") || limpo.Contains("ar") || limpo.Contains("arabic"))
        {
            return "Árabe";
        }
        foreach (var idioma in Idiomas)
        {
            if (limpo.Contains(idioma.Key))
            {
                return idioma.Value;
            }
        }
        return null;
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

        if (_duracaoTotalMs > 0)
        {
            long posicaoMs = (long)(e.Position * _duracaoTotalMs);
            PosicaoEmMilissegundos = posicaoMs;
            TempoAtualFormatado = FormatarTempo(posicaoMs);
        }
    }

    private void OnPlaying(object? sender, EventArgs e)
    {
        Log.Salvar($"EVENT Playing | State={_mediaPlayer.State}");
        IsPlaying = true;
        IsLoading = false;
    }
    private void OnMute(object? sender, EventArgs e)
    {
        Log.Salvar($"EVENT Muted");
        IsMuted = _mediaPlayer.Mute;
    }
    private void OnPaused(object? sender, EventArgs e)
    {
        Log.Salvar("EVENT Paused");
        IsPlaying = false;
    }
    private void OnStopped(object? sender, EventArgs e)
    {
        Log.Salvar("EVENT Stopped");
        IsPlaying = false;
    }
    private void OnEndReached(object? sender, EventArgs e)
    {
        Log.Salvar("EVENT EndReached");
        IsPlaying = false;
    }
    private void OnPlayerBuffering(object? sender, MediaPlayerBufferingEventArgs e)
    {
        System.Windows.Application.Current?.Dispatcher.BeginInvoke(() =>
        {
            float cachePreenchido = e.Cache;

            if (cachePreenchido < 100)
            {
                IsLoading = true;
                //Log.Salvar($"[ALERTA REDE] Preenchendo buffer: {cachePreenchido:0.0}%");
            }
            else
            {
                IsLoading = false;
                Log.Salvar("[ALERTA REDE] Buffer cheio. Continuando reprodução.");
            }
        });
    }
    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
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

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Log.Salvar("Dispose do ViewModel iniciado");

        // Remove imediatamente as inscrições de eventos para evitar callbacks fantasmas
        _mediaPlayer.PositionChanged -= OnPositionChanged;
        _mediaPlayer.Playing -= OnPlaying;
        _mediaPlayer.Paused -= OnPaused;
        _mediaPlayer.Muted -= OnMute;
        _mediaPlayer.Unmuted -= OnMute;
        _mediaPlayer.Stopped -= OnStopped;
        _mediaPlayer.EndReached -= OnEndReached;
        _mediaPlayer.Buffering -= OnPlayerBuffering;

        try
        {
            if (_mediaPlayer.IsPlaying)
            {
                _mediaPlayer.Stop();
            }

            _media?.Dispose();
            _media = null;

            // Descarta o MediaPlayer e depois a instância do LibVLC
            _mediaPlayer.Dispose();
            _libVLC.Dispose();
        }
        catch (Exception ex)
        {
            Log.Salvar($"Erro durante o dispose nativo do VLC: {ex.Message}");
        }

        Log.Salvar("Dispose do ViewModel concluído");
    }

    internal void LoadExternalTorrent(string fileName)
    {
        throw new NotImplementedException();
    }

    public event Action<TorrentDto>? TorrentStatus;
    public void AtualizarTorrent(TorrentDto torrentDto)
    {
        _mediaPlayer.SeekTo(torrentDto.TempoTotal);

        TorrentStatus?.Invoke(torrentDto);
    }    
}

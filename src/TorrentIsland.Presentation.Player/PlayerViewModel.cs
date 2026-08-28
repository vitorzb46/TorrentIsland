using LibVLCSharp.Shared;
using Panlingo.LanguageIdentification.CLD2;
using SubtitlesParser.Classes.Parsers;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows.Input;

namespace TorrentIsland.Presentation.Player;

public sealed class PlayerViewModel : INotifyPropertyChanged, IDisposable
{
    #region Fields
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
    //private System.Threading.Timer? _diagnosticTimer;
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
    #endregion

    #region Constructor
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

            if (_mediaPlayer.Media != null)
            {
                Log.Salvar($"Media Type: {_mediaPlayer.Media.Type}");
                Log.Salvar($"Media State: {_mediaPlayer.Media.State}");
                Log.Salvar($"Media Duration: {_mediaPlayer.Media.Duration}");
                Log.Salvar($"Media Tracks: {_mediaPlayer.Media.Tracks?.Length ?? 0}");
            }

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

        //#if DEBUG
        //        Log.Salvar("DEBUG: TIMER DE SINCRONIA ATIVADO");
        //        _diagnosticTimer = new System.Threading.Timer(
        //        _ =>
        //        {
        //            try
        //            {
        //                //DiagnosticarSincronia("TIMER DE SINCRONIA CTOR - DEBUG");
        //            }
        //            catch { }
        //        },
        //        null,
        //        TimeSpan.FromSeconds(3),
        //        TimeSpan.FromSeconds(5));
        //#endif
    }
    #endregion

    #region Properties
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
        set { _posicaoMs = value; OnPropertyChanged(); TempoAtualFormatado = FormatarTempo(value); }
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
    #endregion

    #region Public Methods
    public void ToggleFullscreen() => IsFullscreen = !IsFullscreen;

    public void SelectAudioTrack(int trackId) => _mediaPlayer.SetAudioTrack(trackId);

    public void SelectSubtitleTrack(int spuId) => _mediaPlayer.SetSpu(spuId);

    public void InicializarDuracaoDoVideo(long totalMilliseconds)
    {
        DuracaoTotalEmMilissegundos = totalMilliseconds;
        TempoTotalFormatado = FormatarTempo(totalMilliseconds);
    }

    public void SetMedia(Media media)
    {
        _media?.Dispose();
        _media = media;
        _mediaPlayer.Play(_media);
        IsLoading = true;
    }
    public void SeekTo(TimeSpan timeSpan)
    {
        if (_duracaoTotalMs > 0)
        {
            Position = (timeSpan.TotalMilliseconds / _duracaoTotalMs) * 100.0;
        }
        _mediaPlayer.SeekTo(timeSpan);
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
    #endregion

    #region Private Methods
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
    private static string FormatarTempo(long milissegundos)
    {
        TimeSpan tempo = TimeSpan.FromMilliseconds(milissegundos);

        if (tempo.TotalHours >= 1)
        {
            return tempo.ToString(@"hh\:mm\:ss");
        }
        return tempo.ToString(@"mm\:ss");
    }

    public async Task PopulateTracksAsync(CancellationToken ct = default)
    {
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
                                                          .ToDictionary(t => t.Id, t => (t.Language, t.Description, t.Codec));

        var caminhoMkvExtract = Path.Combine(AppContext.BaseDirectory, "mkvextract.exe");
        var x = Path.Combine(AppContext.BaseDirectory, "Downloads");
        var caminhoDoVideo = Directory.GetFiles(x, "*.mkv")[0];
        var caminhoTemp = Path.Combine(AppContext.BaseDirectory, "temp");


        Log.Salvar($"Número de legendas: {spuTracks!.Length}");
        foreach (var legenda in spuTracks!)
        {
            var metaSub = metadadosLegendas!.ContainsKey(legenda.Id) ? metadadosLegendas[legenda.Id].Language : null;
            var metaDesc = metadadosLegendas!.ContainsKey(legenda.Id) ? metadadosLegendas[legenda.Id].Description : null;
            var metaCodec = metadadosLegendas!.ContainsKey(legenda.Id) ? metadadosLegendas[legenda.Id].Codec : 0;
            var arquivoDeSaida = string.Empty;

            if (metaSub == "und")
            {
                // Obtem tipo da legenda por codec
                var extensaoSub = "srt";
                if (metaCodec != 0 && _mediaPlayer.Media != null)
                {
                    var codecDesc = _mediaPlayer.Media.CodecDescription(TrackType.Text, metaCodec).ToLower();
                    if (codecDesc.Contains("vtt")) extensaoSub = "vtt";
                    else if (codecDesc.Contains("ssa") || codecDesc.Contains("ass")) extensaoSub = "ass";
                }

                // Extrai legenda com mkvextract
                arquivoDeSaida = Path.Combine(caminhoTemp, $"{metaSub}_{legenda.Id}.{extensaoSub}");
                var args = $"tracks \"{caminhoDoVideo}\" {legenda.Id}:\"{arquivoDeSaida}\"";
                var processInfo = new ProcessStartInfo
                {
                    FileName = caminhoMkvExtract,
                    Arguments = args,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(processInfo);
                process?.WaitForExit();

                if (File.Exists(arquivoDeSaida))
                {
                    // Analisa legenda extraída
                    var parser = new SubParser();
                    using var fileStream = File.OpenRead(arquivoDeSaida);
                    var items = parser.ParseStream(fileStream, Encoding.UTF8);

                    // Armazena primeiras linhas da legenda
                    var sb = new StringBuilder();
                    for (var i = 0; i < Math.Min(15, items.Count); i++)
                    {
                        foreach (var line in items[i].Lines)
                        {
                            if (!string.IsNullOrWhiteSpace(line) && !int.TryParse(line, out _))
                                sb.Append(line).Append(' ');
                        }
                    }

                    // Identifica idioma da legenda
                    using var detector = new CLD2Detector();
                    var lista = detector.PredictLanguage(sb.ToString());

                    foreach (var item in lista)
                    {
                        if (item.Probability > 0.9)
                        {
                            metaSub = item.Language;
                            Log.Salvar($"Legenda inferida: {metaSub}{Environment.NewLine}");
                        }
                    }
                }
            }

            Log.Salvar($"LEGENDAS: {legenda.Name} | {legenda.Id}");
            SubtitleTracks.Add(new TrackItem(legenda.Id, NomeDaFaixa(
                legenda.Name,
                "Legenda",
                legenda.Id,
                metaSub,
                metaDesc)));

            try { File.Delete(arquivoDeSaida); } catch { }
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
    /// converte "Track N" genérico em "Áudio N"/"Legenda N" e preserva descrições reais.
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
                Log.Salvar($"Nome da faixa: {idioma} - {descricaoReal}{Environment.NewLine}");
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
            return $"{tipo} {id} (Indefinido)";
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

    private static string? TraduzirIdioma(string valor)
    {
        if (!EhIdiomaValido(valor)) return null;

        var limpo = valor.Trim().ToLower();

        if (limpo == "pt" || limpo == "por" || limpo == "pt-br" || limpo.Contains("portuguese"))
        {
            return "Português (BR)";
        }

        if (limpo == "en" || limpo == "eng" || limpo.Contains("english"))
        {
            return "Inglês";
        }

        if (limpo == "es" || limpo == "spa" || limpo.Contains("spanish") || limpo.Contains("espanol"))
        {
            return "Espanhol";
        }

        if (limpo == "fr" || limpo == "fre" || limpo == "fra" || limpo.Contains("french"))
        {
            return "Francês";
        }

        if (limpo == "de" || limpo == "ger" || limpo == "deu" || limpo.Contains("german"))
        {
            return "Alemão";
        }

        if (limpo == "ja" || limpo == "jpn" || limpo == "jap" || limpo.Contains("japanese"))
        {
            return "Japonês";
        }

        if (limpo == "ko" || limpo == "kor" || limpo.Contains("korean"))
        {
            return "Coreano";
        }

        if (limpo == "zh" || limpo == "zho" || limpo == "chi" || limpo.Contains("chinese"))
        {
            return "Chinês";
        }

        if (limpo == "ru" || limpo == "rus" || limpo.Contains("russian"))
        {
            return "Russo";
        }

        if (limpo == "ar" || limpo == "ara" || limpo.Contains("arabic"))
        {
            return "Árabe";
        }


        if (Idiomas.TryGetValue(limpo, out var idiomaEncontrado))
        {
            return idiomaEncontrado;
        }


        foreach (var idioma in Idiomas)
        {
            if (limpo.Contains(idioma.Value, StringComparison.CurrentCultureIgnoreCase))
            {
                return idioma.Value;
            }
        }
        return null;
    }
    #endregion

    #region Events Handlers
    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    private void OnMute(object? sender, EventArgs e) => IsMuted = _mediaPlayer.Mute;
    private void OnPaused(object? sender, EventArgs e) => IsPlaying = false;
    private void OnStopped(object? sender, EventArgs e) => IsPlaying = false;
    private void OnEndReached(object? sender, EventArgs e) => IsPlaying = false;
    private void OnPlaying(object? sender, EventArgs e)
    {
        Log.Salvar($"EVENT Playing | State={_mediaPlayer.State}");
        IsPlaying = true;
        IsLoading = false;
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
        }
    }
    #endregion


    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Log.Salvar("Dispose do ViewModel iniciado");

        //#if DEBUG
        //        _diagnosticTimer?.Dispose();
        //#endif
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

    //public void DiagnosticarSincronia(string titulo)
    //{
    //    Log.Salvar($"@***===== {titulo} =====***@");
    //    Log.Salvar("=== DIAGNÓSTICO DE SINCRONIA ===");
    //    Log.Salvar($"MediaPlayer.Time: {_mediaPlayer.Time}");
    //    Log.Salvar($"MediaPlayer.Position: {_mediaPlayer.Position:F6}");
    //    Log.Salvar($"MediaPlayer.Length: {_mediaPlayer.Length}");
    //    Log.Salvar($"DuracaoTotalEmMilissegundos: {DuracaoTotalEmMilissegundos}");
    //    Log.Salvar($"PosicaoEmMilissegundos: {PosicaoEmMilissegundos}");
    //    Log.Salvar($"TempoAtualFormatado: {TempoAtualFormatado}");
    //    Log.Salvar($"TempoTotalFormatado: {TempoTotalFormatado}");
    //    Log.Salvar($"Position (percentual): {Position:F4}%");
    //    Log.Salvar("==============================");
    //}
}

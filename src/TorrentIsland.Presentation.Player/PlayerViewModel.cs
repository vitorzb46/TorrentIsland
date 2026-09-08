using LibVLCSharp.Shared;
using LibVLCSharp.Shared.Structures;
using Panlingo.LanguageIdentification.CLD2;
using SubtitlesParser.Classes.Parsers;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Interop;
using TorrentIsland.Application.Settings;
using static TorrentIsland.Presentation.Player.SubCacheManager;

namespace TorrentIsland.Presentation.Player;

public sealed partial class PlayerViewModel : INotifyPropertyChanged, IDisposable
{
    #region Fields
    private readonly MediaPlayer _mediaPlayer;
    private Media? _media;
    private bool _disposed;
    private int _cliquesAvancar = 0;
    private int _cliquesRetroceder = 0;
    private DateTime _ultimoCliqueAvancar = DateTime.MinValue;
    private DateTime _ultimoCliqueRetroceder = DateTime.MinValue;


    [GeneratedRegex(@".*?(?:s\d+e\d+|\d+x\d+)", RegexOptions.IgnoreCase)]
    private static partial Regex SeasonEpisode();
    #endregion

    #region Constructor
    public PlayerViewModel(LibVLC libVLC, MediaPlayer mediaPlayer)
    {
        Log.Salvar("PlayerViewModel iniciado");
        LibVLC = libVLC;
        _mediaPlayer = mediaPlayer;
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
        AppSettings.LoadingMessageChanged += OnLoadingMessageChanged;
        PlayerWindow.SubtitleDelayChanged += OnSubtitleDelayChanged;
    }
    #endregion

    #region Properties
    public static long SubtitleDelay { get; set; } = 0;
    public ObservableCollection<TrackItem> AudioTracks { get; } = [];
    public ObservableCollection<TrackItem> SubtitleTracks { get; } = [];
    public LibVLC LibVLC { get; }
    public string? TorrentName { get; set; }
    public string? MidiaFilePath { get; set; }

    public string SubtitleMessage
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged();
        }
    } = $"Ressincronizar legenda: 0,000 seg.";

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

            if (value)
            {
                IsPlaying = false;
            }
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

            if (value)
            {
                IsLoading = false;
            }
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

    public string TempoAtualFormatado { get; private set { field = value; OnPropertyChanged(); } } = "00:00:00";

    public string TempoTotalFormatado { get; private set { field = value; OnPropertyChanged(); } } = "00:00:00";

    public string FeedbackTempo { get; private set { field = value; OnPropertyChanged(); } } = "";

    public bool MostrarFeedback { get; private set { field = value; OnPropertyChanged(); } } = false;
    #endregion

    #region Public Methods
    public void ToggleLoading() => IsLoading = !IsLoading;

    public void SelectAudioTrack(int trackId) => _mediaPlayer.SetAudioTrack(trackId);

    public void SelectSubtitleTrack(int spuId) => _mediaPlayer.SetSpu(spuId);

    public void SetReset()
    {
        _mediaPlayer.SetPause(true);
        _mediaPlayer.SetPause(false);
    }

    public void SetPause(bool pause) => _mediaPlayer.SetPause(pause);

    public void InicializarDuracaoDoVideo(long totalMilliseconds)
    {
        DuracaoTotalEmMilissegundos = totalMilliseconds;
        TempoTotalFormatado = FormatarTempo(totalMilliseconds);
    }

    public void SetMedia(Media media)
    {
        _media?.Dispose();
        _media = media;
        _mediaPlayer.Media = media;
        _mediaPlayer.Play(_media);
        _mediaPlayer.SetPause(true);
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
        {
            IsPlaying = false;
            _mediaPlayer.Pause();
        }
        else
        {
            IsPlaying = true;
            _mediaPlayer.Play();
        }
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

    public void ToggleDelaySpu(bool delay)
    {
        long d = 500000;

        if (delay)
        {
            SubtitleDelay += d;
        }
        else
        {
            SubtitleDelay -= d;
        }

        _mediaPlayer.SetSpuDelay(SubtitleDelay);

        double segundos = (double)SubtitleDelay / 1000000;
        SubtitleMessage = $"Ressincronizar legenda: {segundos:0.000;-0.000;0.000} seg.";
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
            if (await Task.WhenAny(Task.Delay(100, ct), esperaInicio).ConfigureAwait(true) == esperaInicio)
            {
                break;
            }
        }

        if (System.Windows.Application.Current is { } app && !app.Dispatcher.CheckAccess())
        {
            await app.Dispatcher.InvokeAsync(() => PopulateTracksAsync(ct)).Task.ConfigureAwait(true);
            return;
        }

        await ProcessarLegendasUndAsync();
    }

    public void LoadExternalSubtitle(object? filePath)
    {
        if (filePath is not string path || !File.Exists(path)) return;
        var subs = SubtitleTracks.Count + 90;
        var uri = new Uri(path).AbsoluteUri;
        var fileName = Path.GetFileNameWithoutExtension(path);
        var match = SeasonEpisode().Match(fileName);

        if (match.Success)
        {
            fileName = String.Concat(match.Value, "...");
        }
        else if (fileName.Length > 20)
        {
            fileName = string.Concat(fileName.AsSpan(0, 20), "...");
        }

        SubtitleTracks.Add(new TrackItem(subs, fileName));
        _mediaPlayer.AddSlave(MediaSlaveType.Subtitle, uri, select: true);
    }
    #endregion

    #region Private Methods
    private void VideoView_BG()
    {
        Utils.AtualizarUI(() =>
        {
            var mainHwnd = new WindowInteropHelper(System.Windows.Application.Current.MainWindow).Handle;
            Utils.VideoView_Background_Black(mainHwnd);
        });
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

    private async Task ProcessarLegendasUndAsync()
    {
        string caminhoMkvExtract = Path.Combine(AppContext.BaseDirectory, "mkvextract.exe");
        string? filePath = string.Empty;

        if (!File.Exists(caminhoMkvExtract))
        {
            Log.Salvar("mkvextract.exe não encontrado!");
            return;
        }

        filePath = TorrentName != null
            ? Path.Combine(AppContext.BaseDirectory, "Downloads", TorrentName!)
            : MidiaFilePath;

        if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
        {
            Log.Salvar("Caminho do vídeo inválido ou arquivo não encontrado.");
            return;
        }

        Media? media = _mediaPlayer.Media;

        if (media == null) return;

        // if (media.Tracks is null or []) return;

        LoadingMessage = "Processando legendas...";
        IsLoading = true;

        TrackDescription[]? audioTracks = _mediaPlayer.AudioTrackDescription;
        TrackDescription[]? spuTracks = _mediaPlayer.SpuDescription;

        if (audioTracks is not { Length: > 0 }) return;
        if (spuTracks is not { Length: > 0 }) return;

        string idiomaUsuario = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
        string caminhoTempBase = Path.Combine(Path.GetTempPath(), ".SubExtract");
        string? extensaoSub = "srt";

        ConcurrentBag<TrackItem> novosTracks = [];
        Dictionary<int, (string? Language, string? Description, uint Codec)>? metadadosLegendas;
        Dictionary<int, string> tempFiles = [];
        List<TrackDescription> undTracks = [];
        List<string> argsList = [];

        metadadosLegendas = media.Tracks
            .Where(m => m.TrackType == TrackType.Text)
            .ToDictionary(t => t.Id, t => (t.Language, t.Description, t.Codec));

        undTracks = [.. spuTracks.Where(t => metadadosLegendas.ContainsKey(t.Id) &&
                                             metadadosLegendas[t.Id].Language == "und")];

        if (undTracks.Count == 0) return;

        // Limpa o diretório temporário se houver resquícios não tratados.
        if (Directory.Exists(caminhoTempBase))
            Directory.Delete(caminhoTempBase, true);

        Directory.CreateDirectory(caminhoTempBase);

        argsList = ["tracks", $"\"{filePath}\""];

        AudioTracks.Clear();

        foreach (var t in audioTracks.Where(t => t.Id >= 0))
        {
            AudioTracks.Add(new TrackItem(t.Id, NomeDaFaixa(t.Name, "Áudio", t.Id)));
        }

        SubtitleTracks.Add(new TrackItem(-99, "Aguardando legendas..."));

        foreach (var track in undTracks)
        {
            var metaCodec = metadadosLegendas.ContainsKey(track.Id) ? metadadosLegendas[track.Id].Codec : 0;
            if (metaCodec != 0)
            {
                var codecDesc = media.CodecDescription(TrackType.Text, metaCodec)?.ToLower() ?? "";
                Log.Salvar($"Codec: {codecDesc}");
                if (codecDesc.Contains("vtt")) extensaoSub = "vtt";
                else if (codecDesc.Contains("ssa") || codecDesc.Contains("ass")) extensaoSub = "ass";
            }

            var nomeArquivo = $"{Guid.NewGuid():N}.{extensaoSub}";
            var arquivoDeSaida = Path.Combine(caminhoTempBase, nomeArquivo);

            var cacheEntry = await GetAsync(filePath, track.Id);

            if (cacheEntry != null)
            {
                novosTracks.Add(new TrackItem(track.Id, cacheEntry.Language));
            }
            else
            {
                tempFiles[track.Id] = arquivoDeSaida;
                argsList.Add($"{track.Id}:\"{arquivoDeSaida}\"");
            }

        }

        if (tempFiles.Count > 0)
        {
            var args = string.Join(' ', argsList);

            var processStartInfo = new ProcessStartInfo
            {
                FileName = caminhoMkvExtract,
                Arguments = args,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(processStartInfo);
            if (process != null)
            {
                string? linha;
                while ((linha = await process.StandardOutput.ReadLineAsync().ConfigureAwait(false)) != null)
                {
                    if (!string.IsNullOrWhiteSpace(linha))
                    {
                        //Log.Salvar($"[mkvextract] {linha}");
                    }
                }

                string erros = await process.StandardError.ReadToEndAsync().ConfigureAwait(false);
                if (!string.IsNullOrWhiteSpace(erros))
                {
                    Log.Salvar($"[mkvextract ERRO] {erros}");
                }

                await process.WaitForExitAsync().ConfigureAwait(false);
            }
            else
            {
                Log.Salvar("Falha ao iniciar o processo do mkvextract.");
            }
            await process!.WaitForExitAsync().ConfigureAwait(false);

            var detector = new CLD2Detector();

            // Analisa legenda extraída
            await Parallel.ForEachAsync(tempFiles, new ParallelOptions { MaxDegreeOfParallelism = 4 }, async (kvp, ct) =>
            {
                var trackId = kvp.Key;
                var sub = kvp.Value;

                try
                {
                    if (!File.Exists(sub))
                    {
                        novosTracks.Add(new TrackItem(trackId, "Desconhecido"));
                        return;
                    }

                    using var fileStream = File.OpenRead(sub);
                    var parser = new SubParser();
                    var items = parser.ParseStream(fileStream);

                    var sb = new StringBuilder();
                    for (var i = 0; i < Math.Min(15, items.Count); i++)
                    {
                        foreach (var line in items[i].Lines)
                        {
                            if (!string.IsNullOrWhiteSpace(line) && !int.TryParse(line, out _))
                                sb.Append(line).Append(' ');
                        }
                    }

                    string detectedLang = "und";

                    if (sb.Length > 0)
                    {
                        var predictions = detector.PredictLanguage(sb.ToString());
                        var best = predictions.OrderByDescending(p => p.Probability).FirstOrDefault();

                        if (best != null && best.Probability > 0.9)
                        {
                            detectedLang = best.Language;
                            await SetAsync(filePath, trackId, detectedLang);
                        }
                        else
                        {
                            detectedLang = "und";
                        }
                    }
                    novosTracks.Add(new TrackItem(trackId, detectedLang));
                }
                catch (Exception ex)
                {
                    novosTracks.Add(new TrackItem(trackId, "und"));
                    Log.Salvar($"Falha ao processar legenda ID {trackId}: {ex.Message}");
                }
                finally
                {
                    try { File.Delete(sub); } catch { }
                }
            });

            detector.Dispose();
        }

        /// Atualiza a coleção na UI
        await Utils.AtualizarUIAsync(async () =>
        {
            SubtitleTracks.Clear();
            var allSubs = novosTracks.ToDictionary(kvp => kvp.Id, kvp => kvp.Name);
            var lista = allSubs.OrderBy(i => !i.Value.Contains(idiomaUsuario, StringComparison.CurrentCultureIgnoreCase))
                                   .ThenBy(i => i.Value, StringComparer.Create(CultureInfo.CurrentCulture, ignoreCase: true))
                                   .ToList();

            foreach (var item in lista)
            {
                SubtitleTracks.Add(new TrackItem(item.Key, NomeDaFaixa(null, "Legenda", item.Key, item.Value)));
            }
        });
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

    private void OnPropertyChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    private void OnMute(object? sender, EventArgs e) => IsMuted = _mediaPlayer.Mute;
    private void OnPaused(object? sender, EventArgs e) => IsPlaying = false;
    private void OnStopped(object? sender, EventArgs e) => IsPlaying = false;
    private void OnEndReached(object? sender, EventArgs e) => IsPlaying = false;
    private void OnPlaying(object? sender, EventArgs e)
    {
        IsPlaying = true;
        VideoView_BG();
    }
    private void OnPlayerBuffering(object? sender, MediaPlayerBufferingEventArgs e)
    {
        System.Windows.Application.Current?.Dispatcher.BeginInvoke(() =>
        {
            if (e.Cache < 100 && !IsPlaying)
            {
                IsLoading = true;
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
        IsLoading = true;
        AudioTracks.Clear();
        SubtitleTracks.Clear();
        VideoView_BG();
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
    private void OnSubtitleDelayChanged(object? sender, string e)
    {
        Utils.AtualizarUI(() =>
        {
            SubtitleMessage = e;
        });
    }
    #endregion


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
        AppSettings.LoadingMessageChanged -= OnLoadingMessageChanged;
        PlayerWindow.SubtitleDelayChanged -= OnSubtitleDelayChanged;

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
            LibVLC.Dispose();
        }
        catch (Exception ex)
        {
            Log.Salvar($"Erro durante o dispose nativo do VLC: {ex.Message}");
        }

        Log.Salvar("Dispose do ViewModel concluído");
    }
}
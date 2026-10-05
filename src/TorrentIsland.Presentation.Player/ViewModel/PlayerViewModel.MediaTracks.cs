using LibVLCSharp.Shared;
using LibVLCSharp.Shared.Structures;
using System.Collections.Concurrent;
using System.Globalization;
using System.IO;
using TorrentIsland.Application.DTOs;
using TorrentIsland.Application.Interfaces;
using TorrentIsland.Infrastructure.Logging;
using TorrentIsland.Infrastructure.Subtitles;
using TorrentIsland.Presentation.Player.Common;

namespace TorrentIsland.Presentation.Player.ViewModel;

public partial class PlayerViewModel
{
    #region Subtitle Processing Records
    private readonly record struct PendingTrack(ulong MkvNumber, int VlcId);

    private sealed record AudioSubtitleMetadata(
        TrackDescription[] AudioTracks,
        List<TrackDescription> UndTracks,
        Dictionary<int, (string? Language, string? Description, uint Codec)> SubtitleMetadata,
        string UserLanguage);

    private sealed record TrackMapping(
        ConcurrentBag<TrackItem> CachedTracks,
        List<PendingTrack> PendingTracks);
    #endregion

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
                await ProcessarLegendasUndAsync().ConfigureAwait(false);
            }, "ProcessarLegendasUndAsync").ConfigureAwait(false);
        }, ct);

        // await ProcessarLegendasUndAsync();
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

    private async Task LoadMediaTracksAsync()
    {
        Utils.VideoView_Background_Black();
        await LoadingStateAsync("Carregando faixas...", async () =>
        {
            SetPause(true);
            await PopulateTracksAsync();
            SetPause(false);
        });
    }

    private async Task ProcessarLegendasUndAsync()
    {
        try
        {
            if (string.IsNullOrEmpty(FilePath) || !File.Exists(FilePath))
            {
                Log.Salvar("Caminho do vídeo inválido ou arquivo não encontrado.");
                return;
            }

            var meta = ResolveAudioSubtitleMetadata();
            if (meta is null) return;

            await PrepareTrackUiAsync(meta);

            var mapping = await BuildTrackMappingAsync(meta);

            await ExtractAndDetectAsync(mapping);

            await PopulateSubtitleTracksAsync(meta.UserLanguage, mapping.CachedTracks);
        }
        catch (Exception ex)
        {
            Log.Salvar($"Erro em ProcessarLegendasUndAsync: {ex.Message} {ex.StackTrace}");
        }
    }

    /// <summary>
    /// Lê tracks de áudio e legenda do MediaPlayer e do Media. Retorna null quando
    /// não há dados suficientes para prosseguir.
    /// </summary>
    private AudioSubtitleMetadata? ResolveAudioSubtitleMetadata()
    {
        if (_media == null) return null;

        var audioTracks = _mediaPlayer.AudioTrackDescription;
        var spuTracks = _mediaPlayer.SpuDescription;

        if (audioTracks is not { Length: > 0 }) return null;
        if (spuTracks is not { Length: > 0 }) return null;

        var subtitleMetadata = _media.Tracks
            .Where(m => m.TrackType == TrackType.Text)
            .ToDictionary(t => t.Id, t => (t.Language, t.Description, t.Codec));

        var undTracks = spuTracks
            .Where(t => subtitleMetadata.ContainsKey(t.Id) &&
                        subtitleMetadata[t.Id].Language == "und")
            .OrderBy(t => t.Id)
            .ToList();

        if (undTracks.Count == 0) return null;

        var userLanguage = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;

        return new AudioSubtitleMetadata(
            audioTracks,
            undTracks,
            subtitleMetadata,
            userLanguage);
    }

    /// <summary>
    /// Popula AudioTracks e o placeholder de SubtitleTracks antes da extração.
    /// </summary>
    private async Task PrepareTrackUiAsync(AudioSubtitleMetadata meta)
    {
        await Utils.AtualizarUIAsync(async () =>
        {
            LoadingMessage = "Processando legendas...";

            AudioTracks.Clear();
            AudioTracks.Add(new TrackItem(-1, "Desativar áudio"));
            foreach (var t in meta.AudioTracks.Where(t => t.Id >= 0))
            {
                AudioTracks.Add(new TrackItem(t.Id, NomeDaFaixa(t.Name, "Áudio", t.Id)));
            }

            SubtitleTracks.Clear();
            SubtitleTracks.Add(new TrackItem(-99, "Aguardando legendas..."));
        });
    }

    /// <summary>
    /// Pareia undTracks (VLC) com os TrackNumbers do MKV e separa as faixas
    /// que já têm cache das que precisam de extração.
    /// </summary>
    private async Task<TrackMapping> BuildTrackMappingAsync(AudioSubtitleMetadata meta)
    {
        var cachedTracks = new ConcurrentBag<TrackItem>();
        var pendingTracks = new List<PendingTrack>();

        var mkvOrdered = Subtitle.GetMetadata(FilePath);

        if (meta.UndTracks.Count != mkvOrdered.Count)
        {
            Log.Salvar($"[Aviso] undTracks={meta.UndTracks.Count} " +
                    $"mkvTracks={mkvOrdered.Count} — mapeamento pode estar errado");
        }

        var n = Math.Min(meta.UndTracks.Count, mkvOrdered.Count);

        for (int i = 0; i < n; i++)
        {
            int vlcId = meta.UndTracks[i].Id;
            ulong mkvNum = mkvOrdered[i].TrackNumber;

            CacheEntry? cacheEntry = await Subtitle.TryGetAsync(FilePath, vlcId);
            if (cacheEntry != null)
            {
                cachedTracks.Add(new TrackItem(vlcId, cacheEntry.Language));
                continue;
            }

            pendingTracks.Add(new PendingTrack(mkvNum, vlcId));
            Log.Salvar($"Track sem cache: {mkvNum}");
        }

        return new TrackMapping(cachedTracks, pendingTracks);
    }

    /// <summary>
    /// Extrai cues das faixas sem cache e roda a detecção de idioma,
    /// acumulando o resultado no bag existente do mapping.
    /// </summary>
    private async Task ExtractAndDetectAsync(TrackMapping mapping)
    {
        if (mapping.PendingTracks.Count == 0) return;

        const int maxChars = 2000;

        HashSet<ulong> mkvNumbers = mapping.PendingTracks.Select(p => p.MkvNumber).ToHashSet();

        var lista = Subtitle.Extraction(FilePath, mkvNumbers);

        Dictionary<ulong, int>? mkvToVlc = mapping.PendingTracks.ToDictionary(p => p.MkvNumber, p => p.VlcId);

        var trackTexts = new Dictionary<int, string>(mkvToVlc.Count);

        foreach (var track in lista)
        {
            if (!mkvToVlc.TryGetValue(track.TrackNumber, out var vlcId))
                continue;

            var text = string.Join("\n", track.Cues
                .Where(c => !string.IsNullOrEmpty(c.Text))
                .Select(c => c.Text));

            if (text.Length > maxChars)
                text = text[..maxChars];

            trackTexts[vlcId] = text;
        }

        await Subtitle.Detection(mapping.CachedTracks, trackTexts, FilePath).ConfigureAwait(false);
    }

    /// <summary>
    /// Ordena as faixas detectadas (idioma do usuário primeiro, depois alfabético)
    /// e popula SubtitleTracks na UI.
    /// </summary>
    private async Task PopulateSubtitleTracksAsync(
        string userLanguage,
        ConcurrentBag<TrackItem> allTracks)
    {
        await Utils.AtualizarUIAsync(async () =>
        {
            SubtitleTracks.Clear();
            SubtitleTracks.Add(new TrackItem(-1, "Desativar legenda"));

            var allSubs = allTracks
                    .ToDictionary(kvp => kvp.Id, kvp => kvp.Name)
                    .OrderBy(i => !i.Value.Contains(userLanguage, StringComparison.CurrentCultureIgnoreCase))
                    .ThenBy(i => i.Value, StringComparer.Create(CultureInfo.CurrentCulture, ignoreCase: true))
                    .ToList();

            foreach (var item in allSubs)
            {
                SubtitleTracks.Add(new TrackItem(item.Key, NomeDaFaixa(null, "Legenda", item.Key, item.Value)));
            }
        });
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
}

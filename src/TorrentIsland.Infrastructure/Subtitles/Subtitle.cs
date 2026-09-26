using System.Collections.Concurrent;
using TorrentIsland.Application.DTOs;
using TorrentIsland.Application.Interfaces;
using TorrentIsland.Infrastructure.Services;
using TorrentIsland.Infrastructure.Subtitles.Detection;
using TorrentIsland.Infrastructure.Subtitles.Extraction;
using TorrentIsland.Infrastructure.Subtitles.Models;

namespace TorrentIsland.Infrastructure.Subtitles;

public sealed class Subtitle
{
    /// <summary>
    /// Lê apenas o cabeçalho do arquivo Matroska/WebM e retorna os metadados das faixas
    /// de legenda.
    /// </summary>
    public static IReadOnlyList<SubtitleTrackMetadata> GetMetadata(string filePath)
    {
        return SubtitleExtractor.GetSubtitleTracksMetadata(filePath);
    }

    /// <summary>
    /// Extrai cues de legendas de um arquivo Matroska/WebM, opcionalmente
    /// restringindo a extração a um conjunto específico de faixas.
    /// </summary> 
    /// <returns>
    /// Lista somente-leitura de <see cref="SubtitleTrack"/>. Faixas sem
    /// cues extraídos aparecem com <see cref="SubtitleTrack.Cues"/> vazio.
    /// </returns>
    public static IReadOnlyList<SubtitleTrack> Extraction(
        string filePath,
        IReadOnlySet<ulong>? trackNumbersFilter = null)
    {
        return SubtitleExtractor.ExtractSubtitles(filePath, trackNumbersFilter);
    }

    /// <summary>
    /// Analisa assincronamente um lote de textos de legendas em paralelo 
    /// para detectar seus respectivos idiomas.
    /// </summary>
    public static async Task Detection(ConcurrentBag<TrackItem> novosTracks,
                                       Dictionary<int, string> trackTexts,
                                       string filePath)
    {
        await TrackLanguageDetector.DetectAsync(novosTracks, trackTexts, filePath);
    }

    /// <summary>
    /// Recupera de forma assíncrona uma entrada do cache e
    /// valida sua integridade contra o arquivo em disco.
    /// </summary>
    public static async Task<CacheEntry?> TryGetAsync(string filePath, int trackId)
    {
        return await SubCacheManager.GetAsync(filePath, trackId);
    }
}
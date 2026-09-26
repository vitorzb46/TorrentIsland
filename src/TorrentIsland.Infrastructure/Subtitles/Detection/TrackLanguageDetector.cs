using Panlingo.LanguageIdentification.CLD2;
using System.Collections.Concurrent;
using TorrentIsland.Application.DTOs;
using TorrentIsland.Infrastructure.Logging;
using TorrentIsland.Infrastructure.Services;

namespace TorrentIsland.Infrastructure.Subtitles.Detection;

internal sealed class TrackLanguageDetector
{
    /// <summary>
    /// Analisa assincronamente um lote de textos de legendas em paralelo para detectar seus respectivos idiomas.
    /// </summary>
    /// <param name="novosTracks">A coleção thread-safe 
    /// onde os resultados da detecção (<see cref="TrackItem"/>) serão adicionados.</param>
    /// <param name="trackTexts">Um dicionário contendo o ID da faixa 
    /// como chave e o texto da legenda extraída como valor.</param>
    /// <param name="filePath">O caminho do arquivo de mídia associado às faixas,
    /// utilizado para persistência em cache.</param>
    /// <returns>Uma <see cref="Task"/> que representa a operação de detecção assíncrona.</returns>
    /// <remarks>
    /// O método ajusta dinamicamente o <see cref="ParallelOptions.MaxDegreeOfParallelism"/>
    /// com base na quantidade de faixas a serem processadas.
    /// Caso ocorra uma falha ao processar uma faixa específica, o idioma será definido como "und" (indeterminado).
    /// </remarks>
    internal static async Task DetectAsync(ConcurrentBag<TrackItem> novosTracks, Dictionary<int, string> trackTexts, string filePath)
    {
        var nLoops = trackTexts.Count switch
        {
            10 => 2,
            30 => 3,
            40 => 4,
            _ => 1
        };

        var detector = new CLD2Detector();

        // Analisa legenda extraída
        await Parallel.ForEachAsync(trackTexts, new ParallelOptions { MaxDegreeOfParallelism = nLoops }, async (kvp, ct) =>
        {
            var trackId = kvp.Key;
            var sub = kvp.Value;

            try
            {
                string detectedLang = await PredictionAsync(filePath, detector, trackId, sub);
                novosTracks.Add(new TrackItem(trackId, detectedLang));
            }
            catch (Exception ex)
            {
                novosTracks.Add(new TrackItem(trackId, "und"));
                Log.Salvar($"Falha ao processar legenda ID {trackId}: {ex.Message}");
            }
        });

        detector.Dispose();
    }

    /// <summary>
    /// Realiza a predição do idioma de um texto de legenda específico utilizando o detector CLD2.
    /// </summary>
    /// <param name="filePath">O caminho do arquivo de mídia associado à faixa.</param>
    /// <param name="detector">A instância do detector <see cref="CLD2Detector"/> reutilizada para a análise.</param>
    /// <param name="trackId">O identificador exclusivo da faixa que está sendo analisada.</param>
    /// <param name="sub">O texto da legenda que será avaliado pelo detector.</param>
    /// <returns>
    /// Uma tarefa que retorna uma string representando o idioma detectado (ex: "en", "pt"). 
    /// Retorna "und" se o texto for vazio, se nenhuma predição for encontrada ou
    /// se a probabilidade de acerto for menor ou igual a 90% (0.9).
    /// </returns>
    /// <remarks>
    /// Se o idioma for detectado com sucesso e com alta confiabilidade (probabilidade > 0.9),
    /// o resultado é salvo de forma assíncrona no <see cref="SubCacheManager"/>.
    /// </remarks>
    private static async Task<string> PredictionAsync(string filePath, CLD2Detector detector, int trackId, string sub)
    {
        string detectedLang = string.Empty;

        if (sub != string.Empty)
        {
            var predictions = detector.PredictLanguage(sub);
            var best = predictions.OrderByDescending(p => p.Probability).FirstOrDefault();

            if (best != null && best.Probability > 0.9)
            {
                detectedLang = best.Language;
                await SubCacheManager.SetAsync(filePath, trackId, detectedLang);
            }
            else
            {
                detectedLang = "und";
            }
        }

        return detectedLang;
    }
}

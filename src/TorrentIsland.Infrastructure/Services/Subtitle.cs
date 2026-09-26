using Panlingo.LanguageIdentification.CLD2;
using SubtitlesParserV2;
using System.Collections.Concurrent;
using System.Text;
using TorrentIsland.Application.DTOs;
using TorrentIsland.Infrastructure.Logging;

namespace TorrentIsland.Infrastructure.Services;

public class Subtitle
{
    public static async Task Detection(ConcurrentBag<TrackItem> novosTracks,
                                       Dictionary<int, string> trackTexts,
                                       string filePath)
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
                string detectedLang = await Prediction(filePath, detector, trackId, sub);
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

    private static async Task<string> Prediction(string filePath, CLD2Detector detector, int trackId, string sub)
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
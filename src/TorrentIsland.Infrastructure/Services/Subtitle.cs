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
                                       Dictionary<int, string> tempFiles,
                                       string filePath)
    {
        var nLoops = tempFiles.Count switch
        {
            10 => 2,
            30 => 3,
            40 => 4,
            _ => 1
        };

        var detector = new CLD2Detector();

        // Analisa legenda extraída
        await Parallel.ForEachAsync(tempFiles, new ParallelOptions { MaxDegreeOfParallelism = nLoops }, async (kvp, ct) =>
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

                bool flowControl = SubParser(sub, out StringBuilder sb);

                if (!flowControl) return;

                string detectedLang = await Prediction(filePath, detector, trackId, sb);

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

    private static bool SubParser(string sub, out StringBuilder sb)
    {
        sb = new StringBuilder();
        using var fileStream = File.OpenRead(sub);
        var items = SubtitleParser.ParseStream(fileStream, Encoding.UTF8);

        if (items == null) return false;

        var list = items.Subtitles.ToList();
        int i = 0;
        foreach (var item in list)
        {
            foreach (var line in item.Lines)
            {
                if (!string.IsNullOrWhiteSpace(line) && !int.TryParse(line, out _))
                    sb.Append(line).Append(' ');
            }
            if (++i > 25) break;
        }

        return true;
    }

    private static async Task<string> Prediction(string filePath, CLD2Detector detector, int trackId, StringBuilder sb)
    {
        string detectedLang = string.Empty;

        if (sb.Length > 0)
        {
            var predictions = detector.PredictLanguage(sb.ToString());
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
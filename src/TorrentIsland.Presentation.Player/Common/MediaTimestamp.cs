using System.IO;
using TorrentIsland.Application.Settings;
using TorrentIsland.Infrastructure.Services;

namespace TorrentIsland.Presentation.Player.Common;

public class MediaTimestamp
{
    private static readonly Dictionary<string, Cache> _cache = [];

    private static string CacheFile { get; set; } = AppSettings.TimeCacheFile;

    public static async Task SaveCache(string caminhoDoVideo, long time)
    {
        if (caminhoDoVideo == null) return;
        var entry = new Cache(
            Hash: KeyGenerator.HashString,
            Time: time
        );
        _cache[caminhoDoVideo] = entry;
        await JsonFiles.SaveToFileAsync(_cache, CacheFile);
    }

    public static async Task<Cache> LoadCache(string caminhoDoVideo)
    {
        _cache.TryGetValue(caminhoDoVideo, out var entry);
        if (!HasCache(caminhoDoVideo, entry!))
        {
            await JsonFiles.SaveToFileAsync(_cache, CacheFile);
            return new Cache("0", 0);
        }
        return entry!;
    }

    public static async Task Load()
    {
        await JsonFiles.LoadFromFileAsync(_cache, CacheFile);
    }

    private static bool HasCache(string videoPath, Cache entry)
    {
        if (entry == null) return false;
        if (!File.Exists(videoPath)) return false;
        return entry.Hash == KeyGenerator.HashString;
    }

    public record Cache(string Hash, long Time);
}
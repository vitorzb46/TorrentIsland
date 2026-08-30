using System.Collections.Concurrent;
using System.IO;
using System.Text.Json;

namespace TorrentIsland.Presentation.Player;

public class SubCacheManager
{
    private static readonly string CacheFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "TorrentIsland",
        "Cache");

    private static readonly string CacheFile = Path.Combine(CacheFolder, "legendas_cache.json");

    private static readonly ConcurrentDictionary<string, CacheEntry> _cache = new();

    private static readonly SemaphoreSlim _semaphore = new(1, 1);

    static SubCacheManager()
    {
        Directory.CreateDirectory(CacheFolder);
        Load();
    }

    public static async Task<CacheEntry?> GetAsync(string caminhoDoVideo, int trackId)
    {
        var key = MakeKey(caminhoDoVideo, trackId);
        _cache.TryGetValue(key, out var entry);
        if (!IsValid(caminhoDoVideo, entry!))
        {
            _cache.TryRemove(key, out _);
            await Save();
            return null;
        }
        return entry;
    }

    public static async Task SetAsync(string caminhoDoVideo, int trackId, string language)
    {
        var entry = new CacheEntry
        {
            Language = language,
            FileSize = new FileInfo(caminhoDoVideo).Length,
            LastWriteTime = File.GetLastWriteTime(caminhoDoVideo),
            DetectionTime = DateTime.Now
        };
        _cache[MakeKey(caminhoDoVideo, trackId)] = entry;
        await Save();
    }

    private static void Load()
    {
        try
        {
            if (!File.Exists(CacheFile)) return;
            var json = File.ReadAllText(CacheFile);
            var data = JsonSerializer.Deserialize<ConcurrentDictionary<string, CacheEntry>>(json);
            if (data != null)
            {
                _cache.Clear();
                foreach (var item in data)
                    _cache[item.Key] = item.Value;
            }
        }
        catch (Exception ex)
        {
            Log.Salvar($"Erro ao carregar o cache de legendas: {ex.Message}");
        }
    }

    private static async Task Save()
    {
        await _semaphore.WaitAsync();
        try
        {
            var json = JsonSerializer.Serialize(_cache, Options);
            await File.WriteAllTextAsync(CacheFile, json);
        }
        catch (Exception ex)
        {
            Log.Salvar($"Erro ao salvar o cache de legendas: {ex.Message}");
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true
    };

    private static string MakeKey(string videoPath, int trackId) => $"{videoPath}|{trackId}";

    private static bool IsValid(string videoPath, CacheEntry entry)
    {
        if (entry == null) return false;
        if (!File.Exists(videoPath)) return false;
        var fi = new FileInfo(videoPath);
        return fi.Length == entry.FileSize && fi.LastWriteTime == entry.LastWriteTime;
    }

    public class CacheEntry
    {
        public string Language { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public DateTime LastWriteTime { get; set; }
        public DateTime DetectionTime { get; set; }
    }
}
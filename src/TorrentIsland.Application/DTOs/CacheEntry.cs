namespace TorrentIsland.Application.Interfaces;

public class CacheEntry
{
    public string Hash { get; set; } = string.Empty;
    public string Language { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public DateTime LastWriteTime { get; set; }
    public DateTime DetectionTime { get; set; }
}
using MonoTorrent;
using MonoTorrent.Client;
using TorrentIsland.Infrastructure.Interfaces;
using static TorrentIsland.Application.Settings.AppSettings;

namespace TorrentIsland.Infrastructure.Services;

public class ManagerFiles : IManagerFiles
{
    public ManagerFiles()
    {
        Directory.CreateDirectory(CacheFolder);
        Directory.CreateDirectory(HtmlsFolder);
        Directory.CreateDirectory(TempFolder);
        Directory.CreateDirectory(DownloadsFolder);
        Directory.CreateDirectory(TorrentsFolder);
        Directory.CreateDirectory(ResourcesFolder);
        if (!File.Exists(ArquivoEngineState)) File.Create(ArquivoEngineState);
        if (!File.Exists(SubCacheFile)) File.WriteAllText(SubCacheFile, "{}");
        if (!File.Exists(TimeCacheFile)) File.WriteAllText(TimeCacheFile, "{}");
    }

    public ITorrentManagerFile ArquivoMaiorPrimeiro(TorrentManager manager) => manager.Files.OrderBy(t => t.Length).Last();
}

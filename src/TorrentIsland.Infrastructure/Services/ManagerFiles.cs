using MonoTorrent;
using MonoTorrent.Client;
using TorrentIsland.Application.Settings;
using TorrentIsland.Infrastructure.Interfaces;

namespace TorrentIsland.Infrastructure.Services;

public class ManagerFiles : IManagerFiles
{
    private string AppDataPath;
    private string CurrentDirectory;
    public string CacheFolder { get; private set; }
    public string DownloadFolder { get; private set; }
    public string TorrentsFolder { get; private set; }
    public ManagerFiles(AppSettings app)
    {
        AppDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), app.PastaProjeto);
        CurrentDirectory = Directory.GetCurrentDirectory();
        CacheFolder = Path.Combine(AppDataPath, app.PastaCache);
        DownloadFolder = Path.Combine(CurrentDirectory, app.PastaDownloads);
        TorrentsFolder = Path.Combine(CurrentDirectory, app.PastaTorrents);
        Directory.CreateDirectory(AppDataPath);
        Directory.CreateDirectory(CacheFolder);
        Directory.CreateDirectory(DownloadFolder);
        Directory.CreateDirectory(TorrentsFolder);
    }

    public ITorrentManagerFile ArquivoMaiorPrimeiro(TorrentManager manager) => manager.Files.OrderBy(t => t.Length).Last();
}

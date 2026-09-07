using MonoTorrent;
using MonoTorrent.Client;
using TorrentIsland.Application.Settings;
using TorrentIsland.Infrastructure.Interfaces;

namespace TorrentIsland.Infrastructure.Services;

public class ManagerFiles : IManagerFiles
{
    private string CurrentDirectory;

    public string AppDataPath { get; private set; }
    public string DownloadFolder { get; private set; }
    public string TorrentsFolder { get; private set; }
    public ManagerFiles()
    {
        AppDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), AppSettings.CurrentFolder);
        CurrentDirectory = Directory.GetCurrentDirectory();
        DownloadFolder = Path.Combine(CurrentDirectory, AppSettings.DownloadsFolder);
        TorrentsFolder = Path.Combine(CurrentDirectory, AppSettings.TorrentsFolder);
        Directory.CreateDirectory(AppDataPath);
        Directory.CreateDirectory(AppSettings.CacheFolder);
        Directory.CreateDirectory(DownloadFolder);
        Directory.CreateDirectory(TorrentsFolder);
        if (!File.Exists(AppSettings.ArquivoEngineState)) File.Create(AppSettings.ArquivoEngineState);
    }

    public ITorrentManagerFile ArquivoMaiorPrimeiro(TorrentManager manager) => manager.Files.OrderBy(t => t.Length).Last();
}

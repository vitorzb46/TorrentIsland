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
    public ManagerFiles(AppSettings app)
    {
        AppDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), AppSettings.PastaProjeto);
        CurrentDirectory = Directory.GetCurrentDirectory();
        DownloadFolder = Path.Combine(CurrentDirectory, app.PastaDownloads);
        TorrentsFolder = Path.Combine(CurrentDirectory, app.PastaTorrents);
        Directory.CreateDirectory(AppDataPath);
        Directory.CreateDirectory(app.PastaCache);
        Directory.CreateDirectory(DownloadFolder);
        Directory.CreateDirectory(TorrentsFolder);
        Directory.CreateDirectory(app.PastaEngineState);
        if (!File.Exists(app.ArquivoEngineState)) File.Create(Path.Combine(app.PastaEngineState, app.ArquivoEngineState));
    }

    public ITorrentManagerFile ArquivoMaiorPrimeiro(TorrentManager manager) => manager.Files.OrderBy(t => t.Length).Last();
}

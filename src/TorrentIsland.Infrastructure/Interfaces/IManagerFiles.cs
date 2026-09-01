using MonoTorrent;
using MonoTorrent.Client;

namespace TorrentIsland.Infrastructure.Interfaces
{
    public interface IManagerFiles
    {
        string AppDataPath { get; }
        string DownloadFolder { get; }
        string TorrentsFolder { get; }

        ITorrentManagerFile ArquivoMaiorPrimeiro(TorrentManager manager);
    }
}
using MonoTorrent;
using MonoTorrent.Client;

namespace TorrentIsland.Infrastructure.Interfaces
{
    public interface IManagerFiles
    {
        string DownloadFolder { get; }
        string TorrentsFolder { get; }

        ITorrentManagerFile ArquivoMaiorPrimeiro(TorrentManager manager);
    }
}
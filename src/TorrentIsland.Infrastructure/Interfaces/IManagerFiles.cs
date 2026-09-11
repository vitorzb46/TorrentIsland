using MonoTorrent;
using MonoTorrent.Client;

namespace TorrentIsland.Infrastructure.Interfaces
{
    public interface IManagerFiles
    {
        ITorrentManagerFile ArquivoMaiorPrimeiro(TorrentManager manager);
    }
}
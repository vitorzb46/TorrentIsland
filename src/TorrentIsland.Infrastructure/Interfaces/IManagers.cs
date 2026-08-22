using MonoTorrent;
using MonoTorrent.Client;
using System.Collections.Concurrent;

namespace TorrentIsland.Infrastructure.Interfaces
{
    public interface IManagers
    {
        ConcurrentDictionary<Guid, TorrentManager> All { get; set; }
        Task<TorrentManager?> ObterManagerIdAsync(Guid id);
        Task<List<TorrentManager>> ObterManagersAsync();
        Task<TorrentManager> StreamingAsync(MagnetLink magnet, string savePath, TorrentSettings settings);
        Task<TorrentManager> TorrentDownloadAsync(MagnetLink magnet, string savePath, TorrentSettings settings);
    }
}
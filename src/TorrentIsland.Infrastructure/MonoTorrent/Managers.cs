using MonoTorrent;
using MonoTorrent.Client;
using System.Collections.Concurrent;
using TorrentIsland.Domain.Exceptions;
using TorrentIsland.Infrastructure.Interfaces;

namespace TorrentIsland.Infrastructure.MonoTorrent;

public class Managers(ClientEngine Engine) : IManagers
{
    private ConcurrentDictionary<Guid, TorrentManager> all = [];

    public ConcurrentDictionary<Guid, TorrentManager> All { get => all; set => all = value; }

    public async Task<IList<TorrentManager>> StreamingAsync(Torrent torrent, string savePath, TorrentSettings settings)
    {
        if (Engine.Torrents.Count >= 1) return Engine.Torrents;
        var result = await Engine.AddStreamingAsync(torrent, savePath, settings).ConfigureAwait(false);
        return [result];
    }

    public async Task<IList<TorrentManager>> StreamingAsync(MagnetLink magnet, string savePath, TorrentSettings settings)
    {
        if (Engine.Torrents.Count >= 1) return Engine.Torrents;
        var result = await Engine.AddStreamingAsync(magnet, savePath, settings).ConfigureAwait(false);
        return [result];
    }

    public async Task<IList<TorrentManager>> TorrentDownloadAsync(Torrent torrent, string savePath, TorrentSettings settings)
    {
        if (Engine.Torrents.Count >= 1) return Engine.Torrents;
        var result = await Engine.AddAsync(torrent, savePath, settings).ConfigureAwait(false);
        return [result];
    }

    public async Task<IList<TorrentManager>> TorrentDownloadAsync(MagnetLink magnet, string savePath, TorrentSettings settings)
    {
        if (Engine.Torrents.Count >= 1) return Engine.Torrents;
        var result = await Engine.AddAsync(magnet, savePath, settings).ConfigureAwait(false);
        return [result];
    }

    public async Task<List<TorrentManager>> ObterManagersAsync() => [.. All.Values];

    public async Task<TorrentManager?> ObterManagerIdAsync(Guid id) =>
        All.TryGetValue(id, out var manager) ? manager : throw new InvalidManagerException();
}

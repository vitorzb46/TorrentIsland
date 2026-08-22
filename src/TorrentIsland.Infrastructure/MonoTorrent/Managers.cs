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

    public async Task<TorrentManager> StreamingAsync(MagnetLink magnet, string savePath, TorrentSettings settings) =>
        await Engine.AddStreamingAsync(magnet, savePath, settings).ConfigureAwait(false);

    public async Task<TorrentManager> TorrentDownloadAsync(MagnetLink magnet, string savePath, TorrentSettings settings) =>
        await Engine.AddAsync(magnet, savePath, settings).ConfigureAwait(false);

    public async Task<List<TorrentManager>> ObterManagersAsync() => [.. All.Values];
    public async Task<TorrentManager?> ObterManagerIdAsync(Guid id)
    {
        return All.TryGetValue(id, out var manager) ? manager : throw new InvalidManagerException();
    }
}

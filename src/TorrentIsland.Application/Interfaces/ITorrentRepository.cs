using TorrentIsland.Domain.Entities;
using TorrentIsland.Domain.Enums;

namespace TorrentIsland.Application.Interfaces;

public interface ITorrentRepository
{
    Task<List<Guid>> AddEngineAsync(Memory<byte> torrentData, bool isStream = false);
    Task<List<Guid>> AddEngineAsync(string magnetOrFolderName, bool isStream = false);
    Task<TorrentEntity?> ObterAsync(Guid id);
    Task StartAllTorrentAsync();
    Task<string> StartStreamAsync(Guid id);
    Task StartTorrentAsync(Guid id);
    IReadOnlyList<(Guid Id, string Nome, TorrentEstado Estado, int Seeds, int Peers)> StreamTorrentEstado();
    Task TrackersAsync(Guid id);
}

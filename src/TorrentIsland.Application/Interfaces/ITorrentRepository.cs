using TorrentIsland.Application.DTOs;
using TorrentIsland.Domain.Entities;
using TorrentIsland.Domain.Enums;

namespace TorrentIsland.Application.Interfaces;

public interface ITorrentRepository
{
    Task<List<Guid>> AddEngineAsync(string magnetOrFolderName, bool isStream = false);
    Task EventsAsync(Guid id);
    Task<TorrentEntity?> ObterAsync(Guid id);
    Task<IReadOnlyDictionary<Guid, TorrentDto>> ObterManagersAsync();
    Task StartAllTorrentAsync();
    Task StartStreamAsync(Guid id);
    Task StartTorrentAsync(Guid id);
    IReadOnlyList<(Guid Id, string Nome, TorrentEstado Estado, int Seeds, int Peers)> StreamTorrentEstado();
    Task TrackersAsync(Guid id);
}

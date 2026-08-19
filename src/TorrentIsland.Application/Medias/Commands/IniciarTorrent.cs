using TorrentIsland.Application.Contracts;
using TorrentIsland.Application.Interfaces;
using TorrentIsland.Domain.Interfaces;

namespace TorrentIsland.Application.Medias.Commands;

public class IniciarTorrent(ITorrentRepository repository, ITrackerService tracker) : IIniciarTorrent
{
    public ITorrentRepository Repository { get; } = repository;
    public ITrackerService Tracker { get; } = tracker;

    public async Task<Guid> StartAsync(string input)
    {
        var id = await Repository.AddEngineAsync(input).ConfigureAwait(false);

        await Repository.TrackersAsync(id).ConfigureAwait(false);

        await Repository.StartTorrentAsync(id).ConfigureAwait(false);

        return id;
    }
}

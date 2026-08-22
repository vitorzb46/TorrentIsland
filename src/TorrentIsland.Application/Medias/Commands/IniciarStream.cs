using TorrentIsland.Application.Contracts;
using TorrentIsland.Application.Interfaces;
using TorrentIsland.Domain.Exceptions;

namespace TorrentIsland.Application.Medias.Commands;

public class IniciarStream(ITorrentRepository repository, IEventHandling handler) : IIniciarStream
{
    private ITorrentRepository Repository { get; } = repository;
    private IEventHandling Handler { get; } = handler;

    public async Task StartAsync(string magnet)
    {
        var id = await Repository.AddEngineAsync(magnet, true).ConfigureAwait(false);

        if (id.Count > 1) throw new InvalidManyManagerException();

        await Repository.TrackersAsync(id[0]).ConfigureAwait(false);
        await Handler.EventsAsync(id[0]).ConfigureAwait(false);
        await Repository.StartStreamAsync(id[0]).ConfigureAwait(false);
    }
}


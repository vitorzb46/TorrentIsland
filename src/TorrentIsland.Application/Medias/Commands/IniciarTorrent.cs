using TorrentIsland.Application.Contracts;
using TorrentIsland.Application.Interfaces;
using TorrentIsland.Domain.Interfaces;

namespace TorrentIsland.Application.Medias.Commands;

public class IniciarTorrent(ITorrentRepository repository, ITorrentLoopRenderer renderer, IEventHandling handler) : IIniciarTorrent
{
    private ITorrentRepository Repository { get; } = repository;
    private ITorrentLoopRenderer Renderer { get; } = renderer;
    private IEventHandling Handler { get; } = handler;

    public async Task StartAsync(string input)
    {
        var torrents = await Repository.AddEngineAsync(input).ConfigureAwait(false);

        foreach (var id in torrents)
        {
            await Repository.TrackersAsync(id).ConfigureAwait(false);

            await Handler.EventsAsync(id).ConfigureAwait(false);

            await Repository.StartTorrentAsync(id).ConfigureAwait(false);
        }

        await Renderer.TorrentInfoRender().ConfigureAwait(false);
    }
}

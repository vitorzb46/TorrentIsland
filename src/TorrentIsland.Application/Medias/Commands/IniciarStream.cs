using TorrentIsland.Application.Contracts;
using TorrentIsland.Application.Interfaces;
using TorrentIsland.Domain.Exceptions;

namespace TorrentIsland.Application.Medias.Commands;

public class IniciarStream(ITorrentRepository repository,
                           IEventHandling handler,
                           IPlayerLauncherService Player,
                           IPlayerMonitorRenderer PlayerMonitor) : IIniciarStream
{
    private readonly ITorrentRepository repository = repository;
    private readonly IEventHandling handler = handler;
    private readonly IPlayerLauncherService player = Player;
    private readonly IPlayerMonitorRenderer playerMonitor = PlayerMonitor;

    public async Task StartAsync(string caminhoOuUrl)
    {
        string stream = await StreamTorrentAsync(caminhoOuUrl).ConfigureAwait(false);
        var process = await player.LaunchPlayerAsync(stream);
        await playerMonitor.MonitorPlayerAsync(process, repository.StreamTorrentEstado).ConfigureAwait(false);
    }

    private async Task<string> StreamTorrentAsync(string caminhoOuUrl)
    {
        var id = await repository.AddEngineAsync(caminhoOuUrl, true).ConfigureAwait(false);

        if (id.Count > 1) throw new InvalidManyManagerException();

        await repository.TrackersAsync(id[0]).ConfigureAwait(false);
        await handler.EventsAsync(id[0]).ConfigureAwait(false);
        var stream = await repository.StartStreamAsync(id[0]).ConfigureAwait(false);
        return stream;
    }
}


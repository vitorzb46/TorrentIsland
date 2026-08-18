using TorrentIsland.Application.DTOs;
using TorrentIsland.Application.Interfaces;

namespace TorrentIsland.Application.Medias.Commands;

public class IniciarTorrent(ITorrentRepository repository)
{
    public ITorrentRepository Repository { get; } = repository;

    public async Task<TorrentDto> StartAsync(Guid id)
    {
        return await Repository.StartAsync(id).ConfigureAwait(false);
    }
}

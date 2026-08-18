using TorrentIsland.Application.DTOs;
using TorrentIsland.Application.Medias.Commands;

namespace TorrentIsland.Application.Contracts;

public class CommandsAndQueries(IniciarTorrent iniciarTorrent)
{
    public IniciarTorrent IniciarTorrent { get; } = iniciarTorrent;

    internal async Task<TorrentDto> ObterStatusAsync(Guid id)
    {
        return await IniciarTorrent.StartAsync(id).ConfigureAwait(false);
    }
}

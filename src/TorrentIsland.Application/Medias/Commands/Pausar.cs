using TorrentIsland.Application.Interfaces;

namespace TorrentIsland.Application.Medias.Commands;

internal class Pausar
{
    internal static async Task TorrentAsync(Guid id, ITorrentRepository repo)
    {
        await repo.PauseTorrentAsync(id).ConfigureAwait(false);
    }
}
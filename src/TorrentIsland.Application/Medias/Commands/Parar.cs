using TorrentIsland.Application.Interfaces;

namespace TorrentIsland.Application.Medias.Commands;

internal class Parar
{
    internal static async Task TorrentAsync(Guid id, ITorrentRepository repo)
    {
        await repo.StopTorrentAsync(id).ConfigureAwait(false);
    }
}

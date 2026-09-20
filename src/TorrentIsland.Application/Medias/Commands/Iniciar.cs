using TorrentIsland.Application.Interfaces;

namespace TorrentIsland.Application.Medias.Commands;

internal class Iniciar
{
    internal static async Task TorrentAsync(Guid id, ITorrentRepository repo)
    {
        await repo.StartTorrentDirectAsync(id).ConfigureAwait(false);
    }
}

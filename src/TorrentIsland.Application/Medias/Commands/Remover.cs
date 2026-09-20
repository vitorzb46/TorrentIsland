using TorrentIsland.Application.Interfaces;

namespace TorrentIsland.Application.Medias.Commands;

internal class Remover
{
    internal static async Task TorrentAsync(Guid id, ITorrentRepository repo) => await repo.RemoveTorrentAsync(id);
}
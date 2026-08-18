using TorrentIsland.Application.DTOs;
using TorrentIsland.Domain.DTOs;
using TorrentIsland.Domain.Entities;

namespace TorrentIsland.Application.Interfaces;

public interface ITorrentRepository
{
    Task<Guid> AdicionarAsync(TorrentCreationInfo info, CancellationToken ct);
    Task<Torrent> ObterPorIdAsync(Guid id);
    Task<TorrentDto> StartAsync(Guid id);
}

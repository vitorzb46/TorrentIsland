using MonoTorrent.Client;
using TorrentIsland.Application.DTOs;
using TorrentIsland.Domain.Entities;

namespace TorrentIsland.Infrastructure.Interfaces
{
    public interface IEntityMapping
    {
        Task<IReadOnlyDictionary<Guid, TorrentDto>> ObterManagersAsync();
        Task RegristoIdAsync(Guid id, TorrentManager manager);
        TorrentEntity ToEntity(Guid id);
    }
}
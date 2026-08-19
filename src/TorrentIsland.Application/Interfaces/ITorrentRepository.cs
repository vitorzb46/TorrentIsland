using TorrentIsland.Application.DTOs;
using TorrentIsland.Domain.DTOs;
using TorrentIsland.Domain.Entities;

namespace TorrentIsland.Application.Interfaces;

public interface ITorrentRepository<TManager> where TManager : class
{
    Task<Guid> AddEngineAsync(string magnetOrFolderName);
    Task<TManager?> ObterManager(Guid id);
    Task<Guid> RegristoIdAsync(Guid id, TManager manager);
    Task StartAllTorrentAsync();
    Task StartTorrentAsync(Guid id);
}

using TorrentIsland.Application.DTOs;

namespace TorrentIsland.Application.Interfaces;

public interface ITorrentService
{
    Task<Guid> CriarTorrentAsync(string input);
    Task<TorrentDto> ObterTorrentAsync(Guid id);
}
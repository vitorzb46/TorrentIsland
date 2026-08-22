using TorrentIsland.Application.DTOs;

namespace TorrentIsland.Application.Interfaces;

public interface ITorrentService
{
    Task CriarTorrentAsync(string input);
    Task StreamTorrentAsync(string input);
    Task<TorrentDto> ObterTorrentAsync(Guid id);
}
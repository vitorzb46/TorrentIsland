using TorrentIsland.Application.DTOs;

namespace TorrentIsland.Application.Interfaces;

public interface ITorrentService
{
    Task<Guid> CriarTorrentAsync(string magnetLink, string savePath, CancellationToken ct);
    Task<TorrentDto> ObterStatusAsync(Guid id);
}
using TorrentIsland.Application.DTOs;

namespace TorrentIsland.Application.Contracts
{
    public interface IObterTorrent
    {
        Task<TorrentDto> TorrentAsync(Guid id);
    }
}
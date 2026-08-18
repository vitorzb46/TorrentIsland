using TorrentIsland.Application.DTOs;
using TorrentIsland.Application.Interfaces;
using TorrentIsland.Domain.Exceptions;

namespace TorrentIsland.Application.Medias.Queries;

public class ObterTorrent(ITorrentRepository repository)
{
    private readonly ITorrentRepository _repository = repository;

    public async Task<TorrentDto> TorrentAsync(Guid id)
    {
        var torrent = await _repository.ObterPorIdAsync(id).ConfigureAwait(false);
        return torrent == null ? throw new InvalidTorrentException() : TorrentDto.FromEntity(torrent);
    }
}

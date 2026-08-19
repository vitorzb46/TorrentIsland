using Microsoft.Extensions.Logging;
using TorrentIsland.Application.DTOs;
using TorrentIsland.Application.Interfaces;
using TorrentIsland.Application.Medias.Commands;
using TorrentIsland.Application.Medias.Queries;

namespace TorrentIsland.Application.Services;

public class TorrentService : ITorrentService
{
    private readonly ITorrentRepository _repository;
    private readonly ILogger<TorrentService> _logger;
    private readonly IniciarTorrent IniciarTorrent;
    private readonly ObterTorrent ObterTorrent;

    public TorrentService(ITorrentRepository repository, ILogger<TorrentService> logger, IniciarTorrent iniciarTorrent, ObterTorrent obterTorrent)
    {
        _repository = repository;
        _logger = logger;
        IniciarTorrent = iniciarTorrent;
        ObterTorrent = obterTorrent;
    }

    public Task<Guid> CriarTorrentAsync(string input)
    {
        var id = IniciarTorrent.StartAsync(input);
        return id;
    }

    public Task<TorrentDto> ObterTorrentAsync(Guid id)
    {
        var torrent = ObterTorrent.TorrentAsync(id);
        return torrent;
    }
}
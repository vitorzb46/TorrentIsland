using Microsoft.Extensions.Logging;
using TorrentIsland.Application.Contracts;
using TorrentIsland.Application.DTOs;
using TorrentIsland.Application.Interfaces;

namespace TorrentIsland.Application.Services;

public class TorrentService(ITorrentRepository repository, ILogger<TorrentService> logger, IIniciarTorrent iniciarTorrent, IObterTorrent obterTorrent) : ITorrentService
{
    private readonly ITorrentRepository _repository = repository;
    private readonly ILogger<TorrentService> _logger = logger;
    private readonly IIniciarTorrent IniciarTorrent = iniciarTorrent;
    private readonly IObterTorrent ObterTorrent = obterTorrent;

    public async Task CriarTorrentAsync(string input)
    {
        await IniciarTorrent.StartAsync(input);
    }

    public Task<TorrentDto> ObterTorrentAsync(Guid id)
    {
        var torrent = ObterTorrent.TorrentAsync(id);
        return torrent;
    }
}
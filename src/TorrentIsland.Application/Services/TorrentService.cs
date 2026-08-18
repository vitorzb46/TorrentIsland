using Microsoft.Extensions.Logging;
using TorrentIsland.Application.Contracts;
using TorrentIsland.Application.DTOs;
using TorrentIsland.Application.Interfaces;
using TorrentIsland.Domain.DTOs;

namespace TorrentIsland.Application.Services;

public class TorrentService(ITorrentRepository repository, CommandsAndQueries commandsAndQueries, ILogger<TorrentService> logger) : ITorrentService
{
    private readonly ITorrentRepository _repository = repository;
    private readonly CommandsAndQueries commandsAndQueries = commandsAndQueries;
    private readonly ILogger<TorrentService> _logger = logger;

    public async Task<Guid> CriarTorrentAsync(string magnetLink, string savePath, CancellationToken ct)
    {
        _logger.LogInformation("Criando torrent para: {MagnetLink}", magnetLink);

        // Cria o DTO do Domain
        var info = new TorrentCreationInfo(magnetLink, savePath);

        // Chama o repositório
        var torrentId = await _repository.AdicionarAsync(info, ct);

        _logger.LogInformation("Torrent criado com ID: {TorrentId}", torrentId);

        return torrentId;
    }
    public async Task<TorrentDto> ObterStatusAsync(Guid id)
    {
        return await commandsAndQueries.ObterStatusAsync(id).ConfigureAwait(false);
    }
}
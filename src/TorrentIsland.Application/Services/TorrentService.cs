using TorrentIsland.Application.Contracts;
using TorrentIsland.Application.DTOs;
using TorrentIsland.Application.Interfaces;
using TorrentIsland.Application.Medias.Commands;

namespace TorrentIsland.Application.Services;

public class TorrentService(
    IIniciarTorrent iniciarTorrent,
    IIniciarStream iniciarStream,
    IObterTorrent obterTorrent,
    ITorrentRepository repository) : ITorrentService
{
    private readonly IIniciarTorrent IniciarTorrent = iniciarTorrent;
    private readonly IIniciarStream IniciarStream = iniciarStream;
    private readonly IObterTorrent ObterTorrent = obterTorrent;
    private readonly ITorrentRepository _repository = repository;

    public async Task CriarTorrentAsync(string input)
    {
        await IniciarTorrent.StartAsync(input);
    }

    public async Task StreamTorrentAsync(string magnet)
    {
        await IniciarStream.StartAsync(magnet);
    }

    public Task<TorrentDto> ObterTorrentAsync(Guid id)
    {
        return ObterTorrent.TorrentAsync(id);
    }

    public async Task RemoverAsync(Guid id)
    {
        await Remover.TorrentAsync(id, _repository);
    }

    public async Task PausarAsync(Guid id)
    {
        await Pausar.TorrentAsync(id, _repository);
    }

    public async Task PararAsync(Guid id)
    {
        await Parar.TorrentAsync(id, _repository);
    }

    public async Task IniciarAsync(Guid id)
    {
        await Iniciar.TorrentAsync(id, _repository);
    }
}

using Microsoft.Extensions.Logging;
using TorrentIsland.Application.Contracts;
using TorrentIsland.Application.DTOs;
using TorrentIsland.Application.Interfaces;

namespace TorrentIsland.Application.Services;

public class TorrentService(
    IIniciarTorrent iniciarTorrent,
    IIniciarStream iniciarStream,
    IObterTorrent obterTorrent) : ITorrentService
{
    private readonly IIniciarTorrent IniciarTorrent = iniciarTorrent;
    private readonly IIniciarStream IniciarStream = iniciarStream;
    private readonly IObterTorrent ObterTorrent = obterTorrent;

    public async Task CriarTorrentAsync(string input)
    {
        await IniciarTorrent.StartAsync(input);
    }

    public async Task CriarStreamAsync(string magnet)
    {
        await IniciarStream.StartAsync(magnet);
    }

    public Task<TorrentDto> ObterTorrentAsync(Guid id)
    {
        var torrent = ObterTorrent.TorrentAsync(id);
        return torrent;
    }
}
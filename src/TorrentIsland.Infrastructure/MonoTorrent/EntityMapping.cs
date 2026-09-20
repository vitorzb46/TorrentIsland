using TorrentIsland.Application.Interfaces;
using TorrentIsland.Domain.Entities;
using TorrentIsland.Infrastructure.DTOs;
using TorrentIsland.Infrastructure.Interfaces;

namespace TorrentIsland.Infrastructure.MonoTorrent;

public class EntityMapping(IFormattingHelper fb) : IEntityMapping
{
    private readonly IFormattingHelper fb = fb;

    public TorrentEntity ToEntity(TorrentDadosBrutos dados)
    {
        var eta = fb.TempoEstimado(dados.TamanhoTotal, dados.BytesRecebidos, dados.VelocidadeDownload, dados.Estado, dados.Progresso);

        var torrent = new TorrentEntity();
        torrent.SetId(dados.Id);
        torrent.SetNome(dados.Nome);
        torrent.SetTamanhoTotal(dados.TamanhoTotal);
        torrent.SetTrackers(dados.Trackers);
        torrent.SetSavePath(dados.SavePath);
        torrent.SetFullPath(dados.FullPath);
        torrent.SetEstado(dados.Estado);
        torrent.SetProgresso(dados.Progresso);
        torrent.SetBytesRecebidos(dados.BytesRecebidos);
        torrent.SetVelocidadeDownload(dados.VelocidadeDownload);
        torrent.SetVelocidadeUpload(dados.VelocidadeUpload);
        torrent.SetSeeds(dados.Seeds);
        torrent.SetParesDisponiveis(dados.ParesDisponiveis);
        torrent.SetTempoEstimado(eta);
        torrent.SetCorEstado();
        return torrent;
    }
}

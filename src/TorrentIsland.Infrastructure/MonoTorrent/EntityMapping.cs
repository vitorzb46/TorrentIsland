using MonoTorrent.Client;
using Spectre.Console;
using System.Collections.ObjectModel;
using TorrentIsland.Application.DTOs;
using TorrentIsland.Domain.Entities;
using TorrentIsland.Infrastructure.Interfaces;

namespace TorrentIsland.Infrastructure.MonoTorrent;

public class EntityMapping(IManagers managers) : IEntityMapping
{
    private IManagers Managers { get; } = managers;

    public async Task RegristoIdAsync(Guid id, TorrentManager manager)
    {
        Managers.All[id] = manager;
        ToEntity(id);
    }

    public async Task<IReadOnlyDictionary<Guid, TorrentDto>> ObterManagersAsync()
    {
        var dtoDict = new Dictionary<Guid, TorrentDto>();

        foreach (var id in Managers.All.Keys)
        {
            var entity = ToEntity(id);
            var dto = TorrentDto.FromEntity(entity);
            dtoDict.Add(id, dto);
        }

        return new ReadOnlyDictionary<Guid, TorrentDto>(dtoDict);
    }

    public TorrentEntity ToEntity(Guid id)
    {
        if (!Managers.All.TryGetValue(id, out var manager))
            throw new KeyNotFoundException("Torrent não encontrado.");

        var Id = id;
        var nome = manager.Torrent?.Name ?? "Torrent desconhecido.";
        var tamanhoTotal = manager.Torrent?.Size ?? 0;
        var trackers = manager.TrackerManager.Tiers.SelectMany(t => t.Trackers)
                                                   .Select(tracker => tracker.Uri.ToString())
                                                   .ToList();
        var savePath = manager.SavePath;
        var estado = manager.Estado();
        var progresso = manager.Progress;
        var bytesRecebidos = manager.Monitor.DataBytesReceived;
        var velocidadeDownload = manager.Monitor.DownloadRate;
        var velocidadeUpload = manager.Monitor.UploadRate;
        var seeds = manager.Peers.Seeds;
        var peers = manager.Peers.Available;
        var bytesRestantes = tamanhoTotal - bytesRecebidos;

        TimeSpan tempoEstimado = TimeSpan.Zero;

        if (velocidadeDownload > 0 && bytesRestantes > 0)
        {
            double segundosRestantes = (double)bytesRestantes / velocidadeDownload;

            // Evita valores absurdos caso a velocidade mude bruscamente
            if (segundosRestantes < double.MaxValue && segundosRestantes > 0)
            {
                tempoEstimado = TimeSpan.FromSeconds(segundosRestantes);
            }
        }
        var eta = tempoEstimado == TimeSpan.Zero ? "Infinito" : tempoEstimado.ToString(@"hh\:mm\:ss");

        var torrent = new TorrentEntity();
        torrent.SetId(id);
        torrent.SetNome(nome);
        torrent.SetTamanhoTotal(tamanhoTotal);
        torrent.SetTrackers(trackers);
        torrent.SetSavePath(savePath);
        torrent.SetEstado(estado);
        torrent.SetProgresso(progresso);
        torrent.SetBytesRecebidos(bytesRecebidos);
        torrent.SetVelocidadeDownload(velocidadeDownload);
        torrent.SetVelocidadeUpload(velocidadeUpload);
        torrent.SetSeeds(seeds);
        torrent.SetParesDisponiveis(peers);
        torrent.SetTempoEstimado(eta);
        torrent.SetCorEstado();
        return torrent;
    }
}

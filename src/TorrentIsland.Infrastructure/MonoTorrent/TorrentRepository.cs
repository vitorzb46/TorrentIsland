using MonoTorrent;
using MonoTorrent.Client;
using System.Collections.Concurrent;
using TorrentIsland.Application.DTOs;
using TorrentIsland.Application.Interfaces;
using TorrentIsland.Application.Settings;
using TorrentIsland.Domain.DTOs;
using Torrent = TorrentIsland.Domain.Entities.Torrent;

namespace TorrentIsland.Infrastructure.MonoTorrent;

public class TorrentRepository : ITorrentRepository
{
    private readonly TorrentSettings _settings;
    private readonly ClientEngine _engine;
    private readonly AppSettings app;
    private readonly ConcurrentDictionary<Guid, TorrentDto> _torrents = [];
    private readonly ConcurrentDictionary<Guid, TorrentManager> _managers = [];

    public TorrentRepository()
    {
        _settings = new TorrentSettingsBuilder
        {
            AllowDht = true,
            AllowInitialSeeding = true,
            AllowPeerExchange = true,
            CreateContainingDirectory = true,
            MaximumConnections = app.ConnectionsMaxima,
            UploadSlots = app.UploadSlotsMaximo,
            MaximumDownloadRate = app.TorrentLimiteDownload,
            MaximumUploadRate = app.TorrentLimiteUpload
        }.ToSettings();
        _engine = new();
    }

    public async Task<Guid> AdicionarAsync(TorrentCreationInfo info, CancellationToken ct)
    {
        var magnet = MagnetLink.Parse(info.MagnetLink);
        var path = Path.Combine(Directory.GetCurrentDirectory(), app.PastaDownloads ?? "Downloads");

        var manager = await _engine.AddAsync(magnet, path, _settings).ConfigureAwait(false);
        var id = Guid.NewGuid();

        _managers[id] = manager;

        await manager.StartAsync().ConfigureAwait(false);

        return id;
    }
    public async Task<Torrent> ObterPorIdAsync(Guid id)
    {
        if (!_managers.TryGetValue(id, out var manager))
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

        var torrent = new Torrent();
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
        return await Task.FromResult(torrent);
    }

    public async Task<TorrentDto> StartAsync(Guid id)
    {
        throw new NotImplementedException();
    }
}
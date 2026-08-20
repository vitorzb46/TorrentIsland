using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using MonoTorrent;
using MonoTorrent.Client;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using TorrentIsland.Application.DTOs;
using TorrentIsland.Application.Interfaces;
using TorrentIsland.Domain.Entities;
using TorrentIsland.Domain.Enums;
using TorrentIsland.Domain.Exceptions;
using TorrentIsland.Domain.Interfaces;
using TorrentIsland.Infrastructure.Configuration;

namespace TorrentIsland.Infrastructure.MonoTorrent;

public class TorrentRepository : ITorrentRepository
{
    #region Fields + Constructor
    private readonly ILogger<TorrentRepository> _logger;
    private readonly Microsoft.Extensions.Localization.IStringLocalizer<TorrentRepository> Localizer;
    private readonly TorrentSettings _settings;
    private readonly ClientEngine _engine;
    private readonly AppSettings app;
    private readonly ConcurrentDictionary<Guid, TorrentDto> _torrents = [];
    private readonly ConcurrentDictionary<Guid, TorrentManager> _managers = [];
    private readonly string SavePath;
    private readonly string PastaTorrents;
    private ITrackerService TrackerService { get; }
    public ITorrentLoopRenderer TorrentLoop { get; }

    public TorrentRepository(ILogger<TorrentRepository> logger, IStringLocalizer<TorrentRepository> localizer, AppSettings app, ITrackerService trackerService, ITorrentLoopRenderer torrentLoop)
    {
        this.app = app;
        TrackerService = trackerService;
        TorrentLoop = torrentLoop;
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
        SavePath = Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), app.PastaDownloads ?? "Downloads")).FullName;
        PastaTorrents = Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), app.PastaTorrents ?? "Downloads")).FullName;
        _logger = logger;
        Localizer = localizer;
    }
    #endregion

    private async Task<Guid> RegristoIdAsync(Guid id, TorrentManager manager)
    {
        _managers[id] = manager;
        _ = await ToEntity(id).ConfigureAwait(false);
        return id;
    }
    public ReadOnlyDictionary<Guid, TorrentManager> Managers
    {
        get
        {
            return new ReadOnlyDictionary<Guid, TorrentManager>(_managers);
        }
    }
    private async Task<List<TorrentManager>> TodosManagers() => [.. _managers.Values];
    private async Task<TorrentManager?> ObterManager(Guid id)
    {
        return _managers.TryGetValue(id, out var manager) ? manager : throw new InvalidManagerException();
    }

    public async Task<TorrentEntity?> ObterAsync(Guid id)
    {
        if (!_managers.ContainsKey(id))
        {
            throw new InvalidManagerException();
        }

        return await ToEntity(id).ConfigureAwait(false);
    }

    public async Task StartTorrentAsync(Guid id)
    {
        var manager = await ObterManager(id);
        await manager!.StartAsync().ConfigureAwait(false);
        await manager.DhtAnnounceAsync().ConfigureAwait(false);
        await manager.LocalPeerAnnounceAsync().ConfigureAwait(false);
        await TorrentLoop.TorrentInfoRender().ConfigureAwait(false);
    }

    public async Task StartAllTorrentAsync()
    {
        List<TorrentManager> listaTorrents = await TodosManagers().ConfigureAwait(false);
        if (listaTorrents.Count == 0) throw new InvalidManagerException();
        listaTorrents.ForEach(async manager => await manager.StartAsync().ConfigureAwait(false));
    }

    public async Task<Guid> AddEngineAsync(string magnetOrFolderName)
    {
        ArgumentNullException.ThrowIfNull(magnetOrFolderName);

        if (magnetOrFolderName.StartsWith("magnet:?"))
        {
            var magnet = MagnetLink.Parse(magnetOrFolderName);
            var manager = await _engine.AddAsync(magnet, SavePath, _settings).ConfigureAwait(false);
            Guid id = Guid.NewGuid();
            return await RegristoIdAsync(id, manager).ConfigureAwait(false);
        }
        else
        {
            var managers = await AddTorrentsAsync().ConfigureAwait(false);
            if (managers.Count == 0)
            {
                throw new CustomException(Localizer["Torrent_NenhumEncontrado", PastaTorrents])!;
            }
            return Guid.Empty;
        }
    }

    public async Task TrackersAsync(Guid id)
    {
        var trackersGithub = await TrackerService!.ObterListaAsync().ConfigureAwait(false);
        var trackersOriginais = _managers[id].TrackerManager.Tiers
            .SelectMany(t => t.Trackers)
            .Select(tracker => tracker.Uri.ToString());
        var trackers = trackersOriginais.Union(trackersGithub).ToList();
        try
        {
            foreach (var tracker in trackers)
            {
                if (Uri.TryCreate(tracker, UriKind.Absolute, out Uri? trackerUri))
                    await _managers[id].TrackerManager.AddTrackerAsync(trackerUri).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
            throw;
        }
    }

    public IReadOnlyList<(Guid Id, string Nome, TorrentEstado Estado, int Seeds, int Peers)> EstadoDosTorrents()
    {
        return [.. _managers.Select(p => (
        p.Key,
        p.Value.Torrent?.Name ?? p.Value.Name ?? "?",
        p.Value.Estado(),
        p.Value.Peers.Seeds,
        p.Value.Peers.Available))];
    }

    private async Task<TorrentEntity> ToEntity(Guid id)
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
        return await Task.FromResult(torrent);
    }

    private async Task<List<TorrentManager>> AddTorrentsAsync()
    {
        var listaDeTorrents = new List<Torrent>();
        Directory.CreateDirectory(PastaTorrents);
        string[] arquivos = Directory.GetFiles(PastaTorrents, "*.torrent");
        var tarefas = arquivos.Select(async arquivo =>
        {
            try
            {
                Torrent torrent = await Torrent.LoadAsync(arquivo).ConfigureAwait(false);
                if (Path.GetExtension(torrent.Name) == ".scr") _logger.LogInformation(Localizer["Torrent_AvisoCache", Path.GetFileName(arquivo)]);
                lock (listaDeTorrents)
                    listaDeTorrents.Add(torrent);
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("torrent", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogInformation(Localizer["Torrent_FalhaCarregar", Path.GetFileName(arquivo)]);
                }
                else
                {
                    _logger.LogInformation(Localizer["Torrent_ErroProcessar", Path.GetFileName(arquivo), ex.Message]);
                }
            }
        });

        await Task.WhenAll(tarefas).ConfigureAwait(false);

        var listaDeManagers = new List<TorrentManager>();
        _logger.LogInformation(Localizer["Torrent_Registrando", listaDeTorrents.Count]);
        foreach (var torrent in listaDeTorrents)
        {
            try
            {
                Guid id = Guid.NewGuid();
                var manager = await _engine.AddAsync(torrent, SavePath, _settings).ConfigureAwait(false);
                await RegristoIdAsync(id, manager).ConfigureAwait(false);
                listaDeManagers.Add(manager);
            }
            catch (Exception ex)
            {
                _logger.LogInformation(Localizer["Torrent_FalhaRegistrar", torrent.Name, ex.Message]);
            }

        }
        return listaDeManagers;
    }

    public async Task EventsAsync(Guid id)
    {
        if (!_managers.TryGetValue(id, out var manager))
        {
            _logger.LogWarning("Torrent ([cyan]{TorrentId}[/]) não encontrado.", id);
            return;
        }

        string Nome() => manager.Torrent?.Name ?? "Torrent desconhecido";

        manager.PeersFound += (o, e) =>
        {
            _logger.LogInformation("{Nome} -> [cyan]{NovosPares}[/] novos pares encontrados ([cyan]{ParesExistentes} existentes[/]).",
                Nome(), e.NewPeers, e.ExistingPeers);
        };

        manager.PeerConnected += (o, e) =>
        {
            _logger.LogDebug("{Nome} -> Par conectado: {Peer} ([cyan]{Direcao}[/]).",
                Nome(), e.Peer, e.Direction);
        };

        manager.PeerDisconnected += (o, e) =>
        {
            _logger.LogDebug("{Nome} -> Par desconectado: {Peer}.", Nome(), e.Peer);
        };

        manager.PieceHashed += (o, e) =>
        {
            _logger.LogDebug("{Nome} -> Peça {PieceIndex} verificada - passou: {HashPassed} (progresso {Progresso:0.0}%).",
                Nome(), e.PieceIndex, e.HashPassed, e.Progress);
        };

        manager.ConnectionAttemptFailed += (o, e) =>
        {
            _logger.LogWarning("{Nome} -> [yellow]Falha de conexão[/] com {Peer}: {Razao}.", Nome(), e.Peer, e.Reason);
        };

        manager.TorrentStateChanged += (o, e) =>
        {
            _logger.LogInformation("{Nome} -> Estado alterado: {Antigo} -> [cyan]{Novo}[/].",
                Nome(), e.OldState, e.NewState);
        };
    }
}
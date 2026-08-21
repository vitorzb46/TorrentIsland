using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using MonoTorrent;
using MonoTorrent.Client;
using MonoTorrent.Streaming;
using Spectre.Console;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using TorrentIsland.Application.DTOs;
using TorrentIsland.Application.Interfaces;
using TorrentIsland.Application.Settings;
using TorrentIsland.Domain.Entities;
using TorrentIsland.Domain.Enums;
using TorrentIsland.Domain.Exceptions;
using TorrentIsland.Domain.Interfaces;

namespace TorrentIsland.Infrastructure.MonoTorrent;

public class TorrentRepository : ITorrentRepository
{
    #region Fields + Constructor
    private readonly Microsoft.Extensions.Localization.IStringLocalizer<TorrentRepository> Localizer;
    private readonly TorrentSettings _settings;
    private readonly ClientEngine _engine;
    private readonly AppSettings app;
    private readonly ConcurrentDictionary<Guid, TorrentDto> _torrents = [];
    private readonly ConcurrentDictionary<Guid, TorrentManager> _managers = [];
    private readonly string SavePath;
    private readonly string PastaTorrents;

    public ILogger<TorrentRepository> Logger { get; }
    private ITrackerService TrackerService { get; }
    public IPlayerLauncherService Player { get; }
    public IPlayerMonitorRenderer PlayerMonitor { get; }

    public TorrentRepository(ClientEngine engine, ILogger<TorrentRepository> logger, IStringLocalizer<TorrentRepository> localizer, AppSettings app, ITrackerService trackerService, IPlayerLauncherService playerLauncher, IPlayerMonitorRenderer playerMonitor)
    {
        this.app = app;
        TrackerService = trackerService;
        Player = playerLauncher;
        PlayerMonitor = playerMonitor;
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
        _engine = engine;
        Logger = logger;
        SavePath = Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), app.PastaDownloads ?? "Downloads")).FullName;
        PastaTorrents = Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), app.PastaTorrents ?? "Downloads")).FullName;
        Localizer = localizer;
    }
    #endregion

    private async Task<List<TorrentManager>> TodosManagers() => [.. _managers.Values];
    private async Task<TorrentManager?> ObterManager(Guid id)
    {
        return _managers.TryGetValue(id, out var manager) ? manager : throw new InvalidManagerException();
    }

    private async Task RegristoIdAsync(Guid id, TorrentManager manager)
    {
        _managers[id] = manager;
        ToEntity(id);
    }

    private async Task<TorrentManager> Streaming(MagnetLink magnet, string savePath, TorrentSettings settings) => 
                                        await _engine.AddStreamingAsync(magnet, savePath, settings).ConfigureAwait(false);
    private async Task<TorrentManager> TorrentDownload(MagnetLink magnet, string savePath, TorrentSettings settings) => 
                                        await _engine.AddAsync(magnet, savePath, settings).ConfigureAwait(false);
    
    public async Task<List<Guid>> AddEngineAsync(string magnetOrFolderName, bool isStream = false)
    {
        ArgumentNullException.ThrowIfNull(magnetOrFolderName);

        if (magnetOrFolderName.StartsWith("magnet:?"))
        {
            var magnet = MagnetLink.Parse(magnetOrFolderName);
            
            var manager = isStream == true ? await Streaming(magnet, SavePath, _settings) : 
                                             await TorrentDownload(magnet, SavePath, _settings);

            await Task.Delay(3000).ConfigureAwait(false);
            while (manager.State == TorrentState.Metadata)
            {
                Logger.LogInformation("Aguardando metadata...");
                await Task.Delay(1000).ConfigureAwait(false);
            }

            var id = new List<Guid> { Guid.NewGuid() };
            await RegristoIdAsync(id[0], manager).ConfigureAwait(false);
            return id;
        }
        else
        {
            var ids = await AddTorrentsAsync().ConfigureAwait(false);
            return ids;
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
            Logger.LogError(ex.Message);
            throw;
        }
    }
    // File
    private static ITorrentManagerFile? ArquivoMaiorPrimeiro(TorrentManager manager) => manager.Files.OrderBy(t => t.Length).Last();

    #region Torrent Methods
    public async Task StartTorrentAsync(Guid id)
    {
        var manager = await ObterManager(id);
        await manager!.StartAsync().ConfigureAwait(false);
        await manager.DhtAnnounceAsync().ConfigureAwait(false);
        await manager.LocalPeerAnnounceAsync().ConfigureAwait(false);
    }

    public async Task StartAllTorrentAsync()
    {
        List<TorrentManager> listaTorrents = await TodosManagers().ConfigureAwait(false);
        if (listaTorrents.Count == 0) throw new InvalidManagerException();
        listaTorrents.ForEach(async manager => await manager.StartAsync().ConfigureAwait(false));
    }

    public async Task<TorrentEntity?> ObterAsync(Guid id)
    {
        if (!_managers.ContainsKey(id))
        {
            throw new InvalidManagerException();
        }

        return ToEntity(id);
    }

    private async Task<List<Guid>> AddTorrentsAsync()
    {
        var listaDeTorrents = new List<Torrent>();
        Directory.CreateDirectory(PastaTorrents);
        string[] arquivos = Directory.GetFiles(PastaTorrents, "*.torrent");
        var tarefas = arquivos.Select(async arquivo =>
        {
            try
            {
                Torrent torrent = await Torrent.LoadAsync(arquivo).ConfigureAwait(false);
                if (Path.GetExtension(torrent.Name) == ".scr") Logger.LogInformation(Localizer["Torrent_AvisoCache", Path.GetFileName(arquivo)]);
                lock (listaDeTorrents)
                    listaDeTorrents.Add(torrent);
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("torrent", StringComparison.OrdinalIgnoreCase))
                {
                    Logger.LogInformation(Localizer["Torrent_FalhaCarregar", Path.GetFileName(arquivo)]);
                }
                else
                {
                    Logger.LogInformation(Localizer["Torrent_ErroProcessar", Path.GetFileName(arquivo), ex.Message]);
                }
            }
        });

        await Task.WhenAll(tarefas).ConfigureAwait(false);

        if (listaDeTorrents.Count == 0)
        {
            throw new CustomException(Localizer["Torrent_NenhumEncontrado", PastaTorrents])!;
        }

        Logger.LogInformation(Localizer["Torrent_Registrando", listaDeTorrents.Count]);
        var ids = new List<Guid>();
        foreach (var torrent in listaDeTorrents)
        {
            for (int i = 0; i < listaDeTorrents.Count; i++)
            {
                try
                {
                    ids.Add(Guid.NewGuid());
                    var manager = await _engine.AddAsync(torrent, SavePath, _settings).ConfigureAwait(false);
                    await RegristoIdAsync(ids[i], manager).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Logger.LogInformation(Localizer["Torrent_FalhaRegistrar", torrent.Name, ex.Message]);
                }
            }
        }
        return ids;
    }
    #endregion

    #region Stream Methods
    // Publics
    public async Task StartStreamAsync(Guid id)
    {
        bool isStreaming = false;
        var manager = await ObterManager(id);
        var torrent = ArquivoMaiorPrimeiro(manager!);
        var stream = isStreaming == false ? await StreamHttp(manager!, torrent!) : 
                                      throw new CustomException("Não é possível iniciar um segundo stream.");

        isStreaming = true;
        await StreamBuffer(manager!);
        var process = await Player.LaunchPlayerAsync(stream.FullUri, CancellationToken.None);
        await PlayerMonitor.MonitorPlayerAsync(process, CancellationToken.None).ConfigureAwait(false);
    }

    public IReadOnlyList<(Guid Id, string Nome, TorrentEstado Estado, int Seeds, int Peers)> StreamTorrentEstado()
    {
        return [.. _managers.Select(p => (
        p.Key,
        p.Value.Torrent?.Name ?? p.Value.Name ?? "?",
        p.Value.Estado(),
        p.Value.Peers.Seeds,
        p.Value.Peers.Available))];
    }
    // Privates
    private async Task<IHttpStream> StreamHttp(TorrentManager manager, ITorrentManagerFile torrent, bool prebuffer = true) => 
                    await manager!.StreamProvider!.CreateHttpStreamAsync(torrent, prebuffer).ConfigureAwait(false);
    private async Task StreamBuffer(TorrentManager manager)
    {
        double buffer = 5.0; // buffer inicial = 5%
        double progresso = manager.Bitfield.PercentComplete;
        if (progresso > buffer) return;
        while (progresso <= buffer)
        {
            progresso = manager.Bitfield.PercentComplete;
            await Task.Delay(1000).ConfigureAwait(false);
        }
    }
    #endregion

    #region Entity/Dto Mapping 
    private TorrentEntity ToEntity(Guid id)
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
        torrent.SetCorEstado();
        return torrent;
    }

    public async Task<IReadOnlyDictionary<Guid, TorrentDto>> ObterManagersAsync()
    {
        var dtoDict = new Dictionary<Guid, TorrentDto>();

        foreach (var id in _managers.Keys)
        {
            var entity = ToEntity(id);
            var dto = TorrentDto.FromEntity(entity);
            dtoDict.Add(id, dto);
        }

        return new ReadOnlyDictionary<Guid, TorrentDto>(dtoDict);
    }
    #endregion

    #region Events
    public async Task EventsAsync(Guid id)
    {
        if (!_managers.TryGetValue(id, out var manager))
        {
            Logger.LogWarning("Torrent ({TorrentId}) não encontrado.", id);
            return;
        }

        string Nome() => Markup.Escape(manager.Torrent?.Name ?? "Torrent desconhecido");

        manager.PeersFound += (o, e) =>
        {
            Logger.LogInformation("{Nome} -> {NovosPares} novos pares encontrados ({ParesExistentes} existentes).",
                Nome(), e.NewPeers, e.ExistingPeers);
        };

        manager.PeerConnected += async (o, e) =>
        {
            Logger.LogDebug("{Nome} -> Par conectado: {Peer} ({Direcao}).",
                Nome(), e.Peer, e.Direction);
            await Task.Delay(500).ConfigureAwait(false);
        };

        manager.PeerDisconnected += async (o, e) =>
        {
            Logger.LogDebug("{Nome} -> Par desconectado: {Peer}.", Nome(), e.Peer);
            await Task.Delay(500).ConfigureAwait(false);
        };

        manager.PieceHashed += async (o, e) =>
        {
            Logger.LogDebug("{Nome} -> Peça {PieceIndex} verificada - passou: {HashPassed} (progresso {Progresso:0.0}%).",
                Nome(), e.PieceIndex, e.HashPassed, e.Progress);
            await Task.Delay(4000).ConfigureAwait(false);
        };

        manager.ConnectionAttemptFailed += async (o, e) =>
        {
            Logger.LogDebug("{Nome} -> [yellow]Falha de conexão[/] com {Peer}: {Razao}.", Nome(), e.Peer, e.Reason);
            await Task.Delay(2000).ConfigureAwait(false);
        };

        manager.TorrentStateChanged += (o, e) =>
        {
            Logger.LogInformation("{Nome} -> Estado alterado: {Antigo} -> {Novo}",
                Nome(), e.OldState, e.NewState);
        };
    }
    #endregion
}
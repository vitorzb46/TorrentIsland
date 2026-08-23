using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using MonoTorrent;
using MonoTorrent.Client;
using MonoTorrent.Streaming;
using Spectre.Console;
using TorrentIsland.Application.Interfaces;
using TorrentIsland.Application.Settings;
using TorrentIsland.Domain.Entities;
using TorrentIsland.Domain.Enums;
using TorrentIsland.Domain.Exceptions;
using TorrentIsland.Domain.Interfaces;
using TorrentIsland.Infrastructure.Interfaces;

namespace TorrentIsland.Infrastructure.MonoTorrent;

public class TorrentRepository : ITorrentRepository
{
    #region Fields + Constructor
    private readonly Microsoft.Extensions.Localization.IStringLocalizer<TorrentRepository> Localizer;

    private ClientEngine Engine { get; set; }
    public AppSettings App { get; set; }
    private IManagerFiles ManagerFiles { get; }
    private IManagers Managers { get; }
    private IEntityMapping Map { get; }
    private ILogger<TorrentRepository> Logger { get; }
    private ITrackerService TrackerService { get; }
    private IPlayerLauncherService Player { get; }
    private IPlayerMonitorRenderer PlayerMonitor { get; }
    internal TorrentSettings Settings { get; private set; }

    public TorrentRepository(ClientEngine engine,
        AppSettings app,
        IManagerFiles managerFiles,
        IManagers managers,
        IEntityMapping map,
        ILogger<TorrentRepository> logger,
        IStringLocalizer<TorrentRepository> localizer,
        ITrackerService trackerService,
        IPlayerLauncherService playerLauncher,
        IPlayerMonitorRenderer playerMonitor)
    {
        TrackerService = trackerService;
        Player = playerLauncher;
        PlayerMonitor = playerMonitor;
        Settings = new TorrentSettingsBuilder
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
        Engine = engine;
        App = app;
        ManagerFiles = managerFiles;
        Managers = managers;
        Map = map;
        Logger = logger;
        Localizer = localizer;
    }
    #endregion


    public async Task<List<Guid>> AddEngineAsync(string magnetOrFolderName, bool isStream = false)
    {
        ArgumentNullException.ThrowIfNull(magnetOrFolderName);

        if (magnetOrFolderName.StartsWith("magnet:?"))
        {
            var magnet = MagnetLink.Parse(magnetOrFolderName);

            var manager = isStream == true ? await Managers.StreamingAsync(magnet, ManagerFiles.DownloadFolder, Settings) :
                                             await Managers.TorrentDownloadAsync(magnet, ManagerFiles.DownloadFolder, Settings);

            await Task.Delay(3000).ConfigureAwait(false);
            while (manager.State == TorrentState.Metadata)
            {
                Logger.LogInformation("Aguardando metadata...");
                await Task.Delay(1000).ConfigureAwait(false);
            }

            var id = new List<Guid> { Guid.NewGuid() };
            await Map.RegristoIdAsync(id[0], manager).ConfigureAwait(false);
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
        var trackersOriginais = Managers.All[id].TrackerManager.Tiers
            .SelectMany(t => t.Trackers)
            .Select(tracker => tracker.Uri.ToString());
        var trackers = trackersOriginais.Union(trackersGithub).ToList();
        try
        {
            foreach (var tracker in trackers)
            {
                if (Uri.TryCreate(tracker, UriKind.Absolute, out Uri? trackerUri))
                    await Managers.All[id].TrackerManager.AddTrackerAsync(trackerUri).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex.Message);
            throw;
        }
    }

    #region Torrent Methods
    public async Task StartTorrentAsync(Guid id)
    {
        var manager = await Managers.ObterManagerIdAsync(id);
        await manager!.StartAsync().ConfigureAwait(false);
        await manager.DhtAnnounceAsync().ConfigureAwait(false);
        await manager.LocalPeerAnnounceAsync().ConfigureAwait(false);
    }

    public async Task StartAllTorrentAsync()
    {
        var listaTorrents = await Managers.ObterManagersAsync().ConfigureAwait(false);
        if (listaTorrents.Count == 0) throw new InvalidManagerException();
        listaTorrents.ForEach(async manager => await manager.StartAsync().ConfigureAwait(false));
    }

    public async Task<TorrentEntity?> ObterAsync(Guid id)
    {
        if (!Managers.All.ContainsKey(id))
        {
            throw new InvalidManagerException();
        }

        return Map.ToEntity(id);
    }

    private async Task<List<Guid>> AddTorrentsAsync()
    {
        var listaDeTorrents = new List<Torrent>();
        string[] arquivos = Directory.GetFiles(ManagerFiles.TorrentsFolder, "*.torrent");
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
            throw new CustomException(Localizer["Torrent_NenhumEncontrado", ManagerFiles.TorrentsFolder])!;
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
                    var manager = await Engine.AddAsync(torrent, ManagerFiles.DownloadFolder, Settings).ConfigureAwait(false);
                    await Map.RegristoIdAsync(ids[i], manager).ConfigureAwait(false);
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
        var manager = await Managers.ObterManagerIdAsync(id);
        var torrent = ManagerFiles.ArquivoMaiorPrimeiro(manager!);
        var stream = App?.OneStream == true ? await StreamHttp(manager!, torrent!) :
                                      throw new CustomException("Não é possível iniciar um segundo stream.");

        App.OneStream = false;
        await StreamBuffer(manager!);
        var process = await Player.LaunchPlayerAsync(stream.FullUri, CancellationToken.None);
        await PlayerMonitor.MonitorPlayerAsync(process, StreamTorrentEstado, CancellationToken.None).ConfigureAwait(false);
    }

    public IReadOnlyList<(Guid Id, string Nome, TorrentEstado Estado, int Seeds, int Peers)> StreamTorrentEstado()
    {
        return [.. Managers.All.Select(p => (
        p.Key,
        p.Value.Torrent?.Name ?? p.Value.Name ?? "?",
        p.Value.Estado(),
        p.Value.Peers.Seeds,
        p.Value.Peers.Available))];
    }
    // Privates
    private static async Task<IHttpStream> StreamHttp(
        TorrentManager manager,
        ITorrentManagerFile torrent,
        bool prebuffer = true) =>
        await manager!.StreamProvider!.CreateHttpStreamAsync(torrent, prebuffer).ConfigureAwait(false);

    private static async Task StreamBuffer(TorrentManager manager)
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

}
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using MonoTorrent;
using MonoTorrent.Client;
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
    private AppSettings App { get; set; }
    private IManagerFiles ManagerFiles { get; }
    private IManagers Managers { get; }
    private IEntityMapping Map { get; }
    private ILogger<TorrentRepository> Logger { get; }
    private ITrackerService TrackerService { get; }

    public TorrentRepository(
        AppSettings app,
        IManagerFiles managerFiles,
        IManagers managers,
        IEntityMapping map,
        ILogger<TorrentRepository> logger,
        IStringLocalizer<TorrentRepository> localizer,
        ITrackerService trackerService)
    {
        TrackerService = trackerService;
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

        if (magnetOrFolderName.StartsWith("magnet:?", StringComparison.OrdinalIgnoreCase) ||
            Path.GetExtension(magnetOrFolderName).Equals(".torrent", StringComparison.OrdinalIgnoreCase))
        {
            object torrentSource = magnetOrFolderName.StartsWith("magnet:?")
                ? (object)Managers.Parse(magnetOrFolderName)
                : (object)await Managers.LoadAsync(magnetOrFolderName);

            IList<TorrentManager> managers;

            managers = isStream switch
            {
                true => torrentSource switch
                {
                    MagnetLink m => await Managers.StreamingAsync(m, ManagerFiles.DownloadFolder),
                    Torrent t => await Managers.StreamingAsync(t, ManagerFiles.DownloadFolder),
                    _ => throw new InvalidOperationException("Tipo de torrent desconhecido.")
                },
                false => torrentSource switch
                {
                    MagnetLink m => await Managers.TorrentDownloadAsync(m, ManagerFiles.DownloadFolder),
                    Torrent t => await Managers.TorrentDownloadAsync(t, ManagerFiles.DownloadFolder),
                    _ => throw new InvalidOperationException("Tipo de torrent desconhecido.")
                }
            };

            //await Task.Delay(3000).ConfigureAwait(false);
            await Managers.AguardarMetadata(managers).ConfigureAwait(false);

            List<Guid> ids = [];
            for (var i = 0; i < managers.Count; i++)
            {
                ids.Add(Guid.NewGuid());
                Managers.RegistroId(ids[i], managers[i]);
            }

            return ids;
        }
        else
        {
            var ids = await Managers.AddTorrentsAsync().ConfigureAwait(false);
            return ids;
        }
    }

    public async Task TrackersAsync(Guid id)
    {
        var trackersGithub = await TrackerService!.ObterListaAsync().ConfigureAwait(false);
        var trackersOriginais = Managers.GetTrackers(id);
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
        Managers.All.TryGetValue(id, out var manager);
        var dadosBrutos = Managers.CriarDadosBrutos(id, manager!);
        return Map.ToEntity(dadosBrutos);
    }
    #endregion

    #region Stream Methods
    // Publics
    public async Task<string> StartStreamAsync(Guid id)
    {
        var manager = await Managers.ObterManagerIdAsync(id);
        await Managers.AguardarMetadata(manager!).ConfigureAwait(false);
        var torrent = ManagerFiles.ArquivoMaiorPrimeiro(manager!);
        var stream = App?.OneStream == true ? await Managers.StreamHttp(manager!, torrent!) :
                                      throw new CustomException("Não é possível iniciar um segundo stream.");

        App.OneStream = false;
        await Managers.StreamBuffer(manager!);
        return stream.FullUri;
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

    #endregion

}
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
using static TorrentIsland.Application.Settings.AppSettings;

namespace TorrentIsland.Infrastructure.MonoTorrent;

public class TorrentRepository : ITorrentRepository
{
    #region Fields + Constructor
    private readonly IStringLocalizer<TorrentRepository> Localizer;
    private readonly IDownloadQueueService _downloadQueue;
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
        ITrackerService trackerService,
        IDownloadQueueService downloadQueue)
    {
        TrackerService = trackerService;
        _downloadQueue = downloadQueue;
        App = app;
        ManagerFiles = managerFiles;
        Managers = managers;
        Map = map;
        Logger = logger;
        Localizer = localizer;
    }
    #endregion
    
    public async Task<List<Guid>> AddEngineAsync(Memory<byte> torrentData, bool isStream = false)
    {
        var (success, managers) = await Managers.TryCreateManagersAsync(torrentData, isStream).ConfigureAwait(false);

        return await Managers.Registro(success, managers).ConfigureAwait(false);
    }

    public async Task<List<Guid>> AddEngineAsync(string magnetOrFolderName, bool isStream = false)
    {
        ArgumentException.ThrowIfNullOrEmpty(magnetOrFolderName);

        var (success, managers) = await Managers.TryCreateManagersAsync(magnetOrFolderName, isStream).ConfigureAwait(false);

        return await Managers.Registro(success, managers).ConfigureAwait(false);
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
                    await Managers.TorrentCollection[id].TrackerManager.AddTrackerAsync(trackerUri).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex.Message);
            throw;
        }
    }

    #region Torrent Methods
    public async Task RemoveTorrentAsync(Guid id)
    {
        if (_downloadQueue.StreamingTorrentId == id)
            await _downloadQueue.ClearStreamingAsync().ConfigureAwait(false);

        await Managers.RemoveTorrentAsync(id).ConfigureAwait(false);
    }

    public async Task PauseTorrentAsync(Guid id)
    {
        await Managers.PauseAsync(id).ConfigureAwait(false);
    }

    public async Task StopTorrentAsync(Guid id)
    {
        await Managers.StopAsync(id).ConfigureAwait(false);
    }

    public async Task StartTorrentDirectAsync(Guid id)
    {
        await Managers.StartAsync(id).ConfigureAwait(false);
    }

    public async Task StartTorrentAsync(Guid id) => await _downloadQueue.EnqueueAsync(id);

    public async Task StartAllTorrentAsync()
    {
        var listaTorrents = await Managers.ObterManagersAsync().ConfigureAwait(false);
        if (listaTorrents.Count == 0) throw new InvalidManagerException();
        listaTorrents.ForEach(async manager => await manager.StartAsync().ConfigureAwait(false));
    }

    public async Task<TorrentEntity?> ObterAsync(Guid id)
    {
        if (!Managers.TorrentCollection.ContainsKey(id))
        {
            throw new InvalidManagerException();
        }
        Managers.TorrentCollection.TryGetValue(id, out var manager);
        var dadosBrutos = Managers.CriarDadosBrutos(id, manager!);
        return Map.ToEntity(dadosBrutos);
    }
    #endregion

    #region Stream Methods
    // Publics
    public async Task<string> StartStreamAsync(Guid id)
    {
        var manager = await Managers.ObterManagerPorIdAsync(id) ?? throw new CustomException("Streaming inválido!");
        var torrent = ManagerFiles.ArquivoMaiorPrimeiro(manager);
        var stream = OneStream == true ? await Managers.StreamHttp(manager!, torrent!) :
                                      throw new CustomException("Não é possível iniciar um segundo stream.");

        OneStream = false;
        await _downloadQueue.SetStreamingAsync(id).ConfigureAwait(false);
        await Managers.StreamBuffer(manager).ConfigureAwait(false);
        return stream.FullUri;
    }

    public IReadOnlyList<(Guid Id, string Nome, TorrentEstado Estado, int Seeds, int Peers)> StreamTorrentEstado()
    {
        return [.. Managers.TorrentCollection.Select(p => (
        p.Key,
        p.Value.Torrent?.Name ?? p.Value.Name ?? "?",
        p.Value.Estado(),
        p.Value.Peers.Seeds,
        p.Value.Peers.Available))];
    }
    #endregion
}
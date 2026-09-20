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
    private readonly Microsoft.Extensions.Localization.IStringLocalizer<TorrentRepository> Localizer;
    private readonly IDownloadQueueService _downloadQueue;
    private static HttpClient? _client;
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
        _client = new HttpClient();
    }
    #endregion

    public async Task<(bool Success, IList<TorrentManager> Manager)> TryCreateManagersAsync(object source, bool isStream)
    {
        object? torrentSource = null;

        try
        {
            switch (source)
            {
                case string stringSource:
                    if (string.IsNullOrWhiteSpace(stringSource))
                        return (false, new List<TorrentManager>());

                    if (stringSource.StartsWith("magnet:?", StringComparison.OrdinalIgnoreCase) ||
                        Path.GetExtension(stringSource).Equals(".torrent", StringComparison.OrdinalIgnoreCase))
                    {
                        torrentSource = stringSource.StartsWith("magnet:?")
                            ? Managers.Parse(stringSource)
                            : Managers.LoadAsync(stringSource);
                    }
                    else
                    {
                        if (Uri.TryCreate(stringSource, UriKind.Absolute, out Uri? uri) &&
                            (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
                        {
                            var torrentTemp = Path.Combine(AppDataFolder, Guid.NewGuid().ToString() + ".torrent");
                            torrentSource = await Managers.LoadAsync(_client!, uri, torrentTemp);
                            File.Delete(torrentTemp);
                        }
                    }
                    break;

                case Memory<byte> memorySource:
                    if (memorySource.IsEmpty || memorySource.Span[0] != (byte)'d')
                        return (false, new List<TorrentManager>());

                    torrentSource = await Managers.LoadAsync(memorySource);
                    break;

                default:
                    return (false, new List<TorrentManager>());

            }

            if (torrentSource == null)
                return (false, new List<TorrentManager>());

            IList<TorrentManager> managers = isStream switch
            {
                true => torrentSource switch
                {
                    MagnetLink m => await Managers.StreamingAsync(m, DownloadsFolder),
                    Torrent t => await Managers.StreamingAsync(t, DownloadsFolder),
                    _ => throw new InvalidOperationException("Tipo de torrent desconhecido.")
                },
                false => torrentSource switch
                {
                    MagnetLink m => await Managers.TorrentDownloadAsync(m, DownloadsFolder),
                    Torrent t => await Managers.TorrentDownloadAsync(t, DownloadsFolder),
                    _ => throw new InvalidOperationException("Tipo de torrent desconhecido.")
                }
            };

            return (true, managers);

        }
        catch (Exception ex)
        {
            throw new CustomException($"O mesmo torrent não pode ser adicionado!");
        }
    }

    private async Task<List<Guid>> Registro(bool success, IList<TorrentManager> managers)
    {
        if (success)
        {
            await Managers.AguardarMetadata(managers).ConfigureAwait(false);

            List<Guid> ids = [];
            for (var i = 0; i < managers.Count; i++)
            {
                ids.Add(Guid.NewGuid());
                Managers.RegistroId(ids[i], managers[i]);
                await _downloadQueue.EnqueueAsync(ids[i]);
            }

            return ids;
        }
        else
        {
            var ids = await Managers.AddTorrentsAsync().ConfigureAwait(false);
            return ids;
        }
    }

    public async Task<List<Guid>> AddEngineAsync(Memory<byte> torrentData, bool isStream = false)
    {
        var (success, managers) = await TryCreateManagersAsync(torrentData, isStream).ConfigureAwait(false);

        return await Registro(success, managers).ConfigureAwait(false);
    }

    public async Task<List<Guid>> AddEngineAsync(string magnetOrFolderName, bool isStream = false)
    {
        ArgumentNullException.ThrowIfNull(magnetOrFolderName);

        var (success, managers) = await TryCreateManagersAsync(magnetOrFolderName, isStream).ConfigureAwait(false);

        return await Registro(success, managers).ConfigureAwait(false);
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
        return [.. Managers.All.Select(p => (
        p.Key,
        p.Value.Torrent?.Name ?? p.Value.Name ?? "?",
        p.Value.Estado(),
        p.Value.Peers.Seeds,
        p.Value.Peers.Available))];
    }
    #endregion
}
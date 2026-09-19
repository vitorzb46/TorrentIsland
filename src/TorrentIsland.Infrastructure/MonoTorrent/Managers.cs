using MonoTorrent;
using MonoTorrent.Client;
using MonoTorrent.Streaming;
using Spectre.Console;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using TorrentIsland.Application.DTOs;
using TorrentIsland.Application.Settings;
using TorrentIsland.Domain.Exceptions;
using TorrentIsland.Infrastructure.DTOs;
using TorrentIsland.Infrastructure.Events;
using TorrentIsland.Infrastructure.Interfaces;
using static TorrentIsland.Application.Settings.AppSettings;

namespace TorrentIsland.Infrastructure.MonoTorrent;

public class Managers : IManagers
{
    private readonly ClientEngine _engine;
    private readonly IEntityMapping _map;

    public ConcurrentDictionary<Guid, TorrentManager> All { get; set; } = [];
    internal TorrentSettings Settings { get; private set; }
    internal TorrentManager? StreamingManager { get; set; }



    public Managers(ClientEngine Engine, AppSettings app, IEntityMapping map)
    {
        _engine = Engine;
        _map = map;
        Settings = Settings = new TorrentSettingsBuilder
        {
            AllowDht = true,
            AllowInitialSeeding = true,
            AllowPeerExchange = true,
            CreateContainingDirectory = true,
            MaximumConnections = app.TorrentConnections,
            UploadSlots = app.UploadSlotsMaximo,
            MaximumDownloadRate = app.TorrentLimiteDownload,
            MaximumUploadRate = app.TorrentLimiteUpload
        }.ToSettings();
    }

    public async Task PauseAsync(Guid id)
    {
        var manager = await ObterManagerPorIdAsync(id);
        if (manager != null && !manager.Complete)
            await manager.PauseAsync().ConfigureAwait(false);
    }

    public async Task PauseAllExceptAsync(Guid id)
    {
        await _engine.PauseAll().ConfigureAwait(false);

        var manager = await ObterManagerPorIdAsync(id);
        if (manager != null && !manager.Complete)
            await manager.StartAsync().ConfigureAwait(false);
    }

    public bool HasTorrentActive(Guid streamingId)
    {
        return _engine.Torrents.Any(tm => tm.State == TorrentState.Downloading
                                || tm.State == TorrentState.Starting
                                || tm.State == TorrentState.Metadata);
    }

    public async Task<bool> TryStart(Guid id)
    {
        var manager = ObterManagerPorId(id);
        if (manager == null || manager.Complete)
            return false;
        
        if (manager.State == TorrentState.Stopped || manager.State == TorrentState.Paused)
        {
            await manager.StartAsync().ConfigureAwait(false);
            await manager.DhtAnnounceAsync().ConfigureAwait(false);
            await manager.LocalPeerAnnounceAsync().ConfigureAwait(false);
            return true;
        }
        return false;
    }

    public async Task SaveEngine()
    {
        await _engine.SaveStateAsync(ArquivoEngineState).ConfigureAwait(false);
    }
    public IEnumerable<string> GetTrackers(Guid id)
    {
        return All[id].TrackerManager.Tiers
            .SelectMany(t => t.Trackers)
            .Select(tracker => tracker.Uri.ToString());
    }

    public async Task<IList<TorrentManager>> StreamingAsync(Torrent torrent, string savePath)
    {
        var result = await AddStreamingAsync(torrent, savePath, Settings).ConfigureAwait(false);
        return [result!];
    }

    public async Task<IList<TorrentManager>> StreamingAsync(MagnetLink magnet, string savePath)
    {
        var result = await AddStreamingAsync(magnet, savePath, Settings).ConfigureAwait(false);
        return [result!];
    }

    public async Task<IList<TorrentManager>> TorrentDownloadAsync(Torrent torrent, string savePath)
    {
        var result = await AddAsync(torrent, savePath, Settings).ConfigureAwait(false);
        return [result];
    }

    public async Task<IList<TorrentManager>> TorrentDownloadAsync(MagnetLink magnet, string savePath)
    {
        var result = await AddAsync(magnet, savePath, Settings).ConfigureAwait(false);
        return [result];
    }

    public async Task<Torrent> LoadAsync(string path) => await Torrent.LoadAsync(path).ConfigureAwait(false);

    public async Task<Torrent> LoadAsync(Memory<byte> data) => await Torrent.LoadAsync(data).ConfigureAwait(false);

    public async Task<Torrent> LoadAsync(HttpClient client, Uri url, string savePath) => await Torrent.LoadAsync(client, url, savePath).ConfigureAwait(false);

    public MagnetLink Parse(string magnet) => MagnetLink.Parse(magnet);

    public async Task<List<TorrentManager>> ObterManagersAsync() => [.. All.Values];
    
    public async Task<TorrentManager?> ObterManagerPorIdAsync(Guid id) => GetManager(id);

    public async Task<IReadOnlyDictionary<Guid, TorrentDto>> ObterTorrentsAsync()
    {
        var dtoDict = new Dictionary<Guid, TorrentDto>();

        foreach (var item in All)
        {
            var id = item.Key;
            var manager = item.Value;

            var dadosBrutos = CriarDadosBrutos(id, manager);

            var entity = _map.ToEntity(dadosBrutos);
            var dto = TorrentDto.FromEntity(entity);
            dtoDict.Add(id, dto);
        }

        return new ReadOnlyDictionary<Guid, TorrentDto>(dtoDict);
    }

    public void RegistroId(Guid id, TorrentManager manager)
    {
        All[id] = manager;
        var dadosBrutos = CriarDadosBrutos(id, manager);
        _map.ToEntity(dadosBrutos);
    }

    public async Task AguardarMetadata(TorrentManager manager) => await AguardarMetadata([manager]).ConfigureAwait(false);
    public async Task AguardarMetadata(IList<TorrentManager> managers)
    {
        foreach (var manager in managers.Where(m => m.State == TorrentState.Stopped))
        {
            await manager.StartAsync().ConfigureAwait(false);
        }
        await Task.Delay(500).ConfigureAwait(false);
        while (managers.Any(m => m.State == TorrentState.Metadata || m.State == TorrentState.Stopped || m.State == TorrentState.Hashing))
        {
            Loading.Message = "Aguardando Metadata...";
            await Task.Delay(1000).ConfigureAwait(false);
        }
    }

    public async Task<IHttpStream> StreamHttp(
        TorrentManager manager,
        ITorrentManagerFile torrent,
        bool prebuffer = true) =>
        await manager!.StreamProvider!.CreateHttpStreamAsync(torrent, prebuffer).ConfigureAwait(false);

    public async Task StreamBuffer(TorrentManager manager)
    {
        double buffer = 5.0;
        double progresso = manager.Bitfield.PercentComplete;
        if (progresso > buffer) return;
        while (progresso <= buffer)
        {
            Loading.Message = $"Buffering {progresso / 100:P2}...";
            progresso = manager.Bitfield.PercentComplete;
            await Task.Delay(Random.Shared.Next(200, 401));
        }

        Loading.Message = "Iniciando streaming...";
    }

    public async Task<List<Guid>> AddTorrentsAsync()
    {
        var listaDeTorrents = new List<Torrent>();
        string[] arquivos = Directory.GetFiles(TorrentsFolder, "*.torrent");
        var tarefas = arquivos.Select(async arquivo =>
        {
            try
            {
                var torrent = await LoadAsync(arquivo).ConfigureAwait(false);
                if (Path.GetExtension(torrent.Name) == ".scr")
                    // Logger.LogInformation(Localizer["Torrent_AvisoCache", Markup.Escape(Path.GetFileName(arquivo))]);
                    return;
                lock (listaDeTorrents)
                    listaDeTorrents.Add(torrent);
            }
            catch (Exception ex)
            {
                var arquivoEscapado = Markup.Escape(Path.GetFileName(arquivo));
                var mensagemEscapada = Markup.Escape(ex.Message);

                if (ex.Message.Contains("torrent", StringComparison.OrdinalIgnoreCase))
                {
                    // Logger.LogInformation(Localizer["Torrent_FalhaCarregar", arquivoEscapado]);
                }
                else
                {
                    // Logger.LogInformation(Localizer["Torrent_ErroProcessar", arquivoEscapado, mensagemEscapada]);
                }
            }
        });

        await Task.WhenAll(tarefas).ConfigureAwait(false);

        if (listaDeTorrents.Count == 0)
        {
            throw new CustomException("Nehuma torrent encontrado!")!;
        }

        // Logger.LogInformation(Localizer["Torrent_Registrando", listaDeTorrents.Count]);
        var ids = new List<Guid>();
        var i = 0;
        foreach (var torrent in listaDeTorrents)
        {
            try
            {
                ids.Add(Guid.NewGuid());
                var manager = await AddAsync(torrent, TorrentsFolder, Settings).ConfigureAwait(false);
                RegistroId(ids[i], manager);
            }
            catch (Exception ex)
            {
                // Logger.LogInformation(Localizer["Torrent_FalhaRegistrar", torrent.Name, ex.Message]);
            }
            i++;
        }
        return ids;
    }

    public TorrentDadosBrutos CriarDadosBrutos(Guid id, TorrentManager manager)
    {
        return new TorrentDadosBrutos(
            Id: id,
            Nome: manager.Torrent?.Name ?? string.Empty,
            TamanhoTotal: manager.Torrent?.Size ?? 0,
            Trackers: manager.TrackerManager.Tiers.SelectMany(t => t.Trackers).Select(tracker => tracker.Uri.ToString()).ToList(),
            SavePath: manager.SavePath,
            FullPath: manager.Files.Select(f => f.FullPath).FirstOrDefault() ?? string.Empty,
            Estado: manager.Estado(),
            Progresso: manager.Progress,
            BytesRecebidos: manager.Monitor.DataBytesReceived,
            VelocidadeDownload: manager.Monitor.DownloadRate,
            VelocidadeUpload: manager.Monitor.UploadRate,
            Seeds: manager.Peers.Seeds,
            ParesDisponiveis: manager.Peers.Available,
            BytesRestantes: (manager.Torrent?.Size ?? 0) - manager.Monitor.DataBytesReceived
        );
    }

    TorrentManager ObterManagerPorId(Guid id) => GetManager(id);

    TorrentManager GetManager(Guid id) => 
        All.TryGetValue(id, out var manager) ? manager : null!;

    async Task<TorrentManager> AddAsync(Torrent torrent, string savePath, TorrentSettings settings)
    {
        return await _engine.AddAsync(torrent, savePath, settings).ConfigureAwait(false);
    }

    async Task<TorrentManager> AddAsync(MagnetLink magnet, string savePath, TorrentSettings settings)
    {
        return await _engine.AddAsync(magnet, savePath, settings).ConfigureAwait(false);
    }

    async Task<TorrentManager?> AddStreamingAsync(Torrent torrent, string savePath, TorrentSettings settings)
    {
        if (await IsStreamning()) return StreamingManager;
        StreamingManager = await _engine.AddStreamingAsync(torrent, savePath, settings).ConfigureAwait(false);
        return StreamingManager;
    }

    async Task<TorrentManager?> AddStreamingAsync(MagnetLink magnet, string savePath, TorrentSettings settings)
    {
        if (await IsStreamning()) return StreamingManager;
        StreamingManager = await _engine.AddStreamingAsync(magnet, savePath, settings).ConfigureAwait(false);
        return StreamingManager;
    }

    private async Task<bool> IsStreamning()
    {
        if (_engine.Torrents.Count == 0) return false;
        var sp = _engine.Torrents.Any(z => z.StreamProvider == null);
        if (sp) return false;

        if (!sp)
        {
            if (StreamingManager != null)
            {
                await StreamingManager.StopAsync().ConfigureAwait(false);
                await _engine.RemoveAsync(StreamingManager, RemoveMode.KeepAllData).ConfigureAwait(false);
                StreamingManager = null;
                return false;
            }
            else
            {
                StreamingManager ??= _engine.Torrents.FirstOrDefault(z => z.StreamProvider != null);
                return true;
            }
        }
        return false;
    }
}

using Microsoft.Extensions.Logging;
using MonoTorrent;
using MonoTorrent.Client;
using System.Collections.Concurrent;
using TorrentIsland.Application.DTOs;
using TorrentIsland.Application.Interfaces;
using TorrentIsland.Application.Settings;
using TorrentIsland.Domain.DTOs;
using TorrentIsland.Domain.Entities;
using TorrentIsland.Domain.Exceptions;

namespace TorrentIsland.Infrastructure.MonoTorrent;
public class TorrentRepository<TManager>
{
    private readonly ILogger<TorrentRepository<TManager>> _logger;
    private readonly Microsoft.Extensions.Localization.IStringLocalizer<TorrentRepository<TManager>> Localizer;
    private readonly TorrentSettings _settings;
    private readonly ClientEngine _engine;
    private readonly AppSettings app;
    private readonly ConcurrentDictionary<Guid, TorrentDto> _torrents = [];
    private readonly ConcurrentDictionary<Guid, TorrentManager> _managers = [];
    private readonly string SavePath;
    private readonly string PastaTorrents;

    public TorrentRepository()
    {
        _settings = new TorrentSettingsBuilder
        {
            AllowDht = true,
            AllowInitialSeeding = true,
            AllowPeerExchange = true,
            CreateContainingDirectory = true,
            MaximumConnections = app!.ConnectionsMaxima,
            UploadSlots = app.UploadSlotsMaximo,
            MaximumDownloadRate = app.TorrentLimiteDownload,
            MaximumUploadRate = app.TorrentLimiteUpload
        }.ToSettings();
        _engine = new();
        SavePath = Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), app.PastaDownloads ?? "Downloads")).FullName;
        PastaTorrents = Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), app.PastaTorrents ?? "Downloads")).FullName;
    }
    public async Task<Guid> RegristoIdAsync(Guid id, TorrentManager manager)
    {
        _managers[id] = manager;
        _ = await ToEntity(id).ConfigureAwait(false);
        return id;
    }

    public async Task<List<TorrentManager>> TodosManagersAsync() => [.. _managers.Values];
    public async Task<TorrentManager?> ObterManager(Guid id)
    {
        return _managers.TryGetValue(id, out var manager) ? manager : throw new InvalidManagerException();
    }

    public async Task StartTorrentAsync(Guid id)
    {
        var manager = await ObterManager(id);
        await manager!.StartAsync().ConfigureAwait(false);
    }

    public async Task StartAllTorrentAsync()
    {
        List<TorrentManager> listaTorrents = await TodosManagersAsync().ConfigureAwait(false);
        if (listaTorrents.Count == 0) throw new InvalidManagerException();
        listaTorrents.ForEach(async manager => await manager.StartAsync().ConfigureAwait(false));
    }

    public async Task<Guid> AddEngineAsync(string magnetOrFolderName)
    {
        ArgumentNullException.ThrowIfNull(magnetOrFolderName);

        if (magnetOrFolderName.StartsWith("magnet"))
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
            //Event Handler
            //MainLoop
        }
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
}
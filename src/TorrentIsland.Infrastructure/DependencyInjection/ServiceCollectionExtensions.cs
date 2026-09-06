using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MonoTorrent.Client;
using MonoTorrent.Dht;
using MonoTorrent.PieceWriter;
using System.Net;
using TorrentIsland.Application.Contracts;
using TorrentIsland.Application.Interfaces;
using TorrentIsland.Application.Medias.Commands;
using TorrentIsland.Application.Medias.Queries;
using TorrentIsland.Application.Services;
using TorrentIsland.Application.Settings;
using TorrentIsland.Domain.Interfaces;
using TorrentIsland.Infrastructure.Configuration;
using TorrentIsland.Infrastructure.Interfaces;
using TorrentIsland.Infrastructure.Logging;
using TorrentIsland.Infrastructure.MonoTorrent;
using TorrentIsland.Infrastructure.Services;
using TorrentIsland.Infrastructure.VLC;

namespace TorrentIsland.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registra logging, configuração e os serviços de torrent (Application + Infrastructure).
    /// </summary>
    public static IServiceCollection AddTorrentIsland(this IServiceCollection services, IConfiguration configuration, IConsoleLogRenderer renderer, int maxLogs = 10)
    {
        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.AddRingBuffer(renderer, maxLogs);
            builder.AddFileLogger();
        });

        services.AddLocalization();
        services.AddSingleton(AppSettingsProvider.Carregar(configuration));

        services.AddSingleton<ClientEngine>(sp =>
        {
            var engineState = sp.GetRequiredService<AppSettings>().ArquivoEngineState;
            var pasta = sp.GetRequiredService<AppSettings>().PastaEngineState;

            try
            {
                if (File.Exists(Path.Combine(pasta, engineState)))
                {
                    return ClientEngine.RestoreStateAsync(engineState).GetAwaiter().GetResult();
                }
            }
            catch (Exception) { }

            var settingBuilder = GetSettingBuilder();
            EngineSettings settings = settingBuilder.ToSettings();
            return Task.Run(() => new ClientEngine(settings)).GetAwaiter().GetResult();
            //return new ClientEngine(settings);
        });
        services.AddSingleton<IEventHandling, EventHandling>();
        services.AddSingleton<IIniciarTorrent, IniciarTorrent>();
        services.AddSingleton<IIniciarStream, IniciarStream>();
        services.AddSingleton<IObterTorrent, ObterTorrent>();
        services.AddSingleton<ITrackerService, TrackerService>();
        services.AddSingleton<IManagerFiles, ManagerFiles>();
        services.AddSingleton<IManagers, Managers>();
        services.AddSingleton<IEntityMapping, EntityMapping>();
        services.AddSingleton<ITorrentRepository, TorrentRepository>();
        services.AddSingleton<ITorrentService, TorrentService>();
        services.AddSingleton<IPlayerLauncherService, PlayerLauncherService>();
        services.AddSingleton<IDLService, DLService>();

        return services;
    }

    private static EngineSettingsBuilder GetSettingBuilder()
    {
        var settings = new AppSettings();

        return new EngineSettingsBuilder
        {
            // --- REDE E CONEXÕES ---
            AllowPortForwarding = settings.RedirecionarPorta,
            AllowLocalPeerDiscovery = settings.DescobertaPeerLocal,
            DhtEndPoint = settings.IpV4, // Porta UDP dinâmica para DHT
            ListenEndPoints = new Dictionary<string, IPEndPoint>
                {
                    { "ipv4", settings.IpV4 }, // Porta TCP/UDP dinâmica para Peers
                    { "ipv6", settings.IpV6 }
                },
            MaximumConnections = settings.ConnectionsMaxima,
            ConnectionRetryDelays = settings.Retry,
            ConnectionTimeouts = settings.PeerTimeout,
            DhtBootstrapRouters = DhtRouter(),

            // --- CACHE E ARQUIVOS ---
            AutoSaveLoadFastResume = settings.LoadFastResume,
            AutoSaveLoadMagnetLinkMetadata = settings.LoadMagnetLinkMetadata,
            AutoSaveLoadDhtCache = settings.LoadDhtCache,
            CacheDirectory = settings.PastaCache,
            UsePartialFiles = settings.ArquivoParcial, // Desativado para ajudar o VLC a ler o arquivo direto
            DiskCachePolicy = CachePolicy.ReadsAndWrites,
            DiskCacheBytes = settings.CacheBytesEmDisco, // 150MB de RAM dedicada a cache

            // --- STREAMING E WEBSEEDS ---
            HttpStreamingPrefix = settings.StreamingPrefix,
            WebSeedConnectionTimeout = settings.ConexaoTimeout,
            WebSeedSpeedTrigger = 0 // Baixa de fontes HTTP e P2P simultaneamente se disponível

        };
    }

    private static List<BootstrapRouter> DhtRouter()
    {
        return
        [
            new("router.bittorrent.com", 6881),
            new("router.utorrent.com", 6881),
            new("router.transmissionbt.com", 6881),
            new("router.bitcomet.com", 6881),
            new("dht.transmissionbt.com", 6881)
        ];
    }
}

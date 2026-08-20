using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MonoTorrent.Client;
using System.Net;
using System.Net.Sockets;
using TorrentIsland.Application.Contracts;
using TorrentIsland.Application.Interfaces;
using TorrentIsland.Application.Medias.Commands;
using TorrentIsland.Application.Services;
using TorrentIsland.Application.Settings;
using TorrentIsland.Domain.Interfaces;
using TorrentIsland.Infrastructure.Configuration;
using TorrentIsland.Infrastructure.Logging;
using TorrentIsland.Infrastructure.MonoTorrent;
using static System.Collections.Generic.Dictionary<TKey, TValue>;

namespace TorrentIsland.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registra logging, configuração e os serviços de torrent (Application + Infrastructure).
    /// </summary>
    public static IServiceCollection AddTorrentIsland(this IServiceCollection services, IConfiguration configuration, int maxLogs = 10)
    {
        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.AddRingBuffer(maxLogs);
            builder.AddFileLogger();
        });

        services.AddLocalization();
        services.AddSingleton(AppSettingsProvider.Carregar(configuration));

        services.AddSingleton<ClientEngine>(sp =>
        {
            var settingBuilder = GetSettingBuilder();
            var config = new AppSettings();
            settingBuilder.DhtBootstrapRouters = config.Router();
            EngineSettings settings = settingBuilder.ToSettings();
            return new ClientEngine(settings);
        });

        services.AddSingleton<TorrentRepository>();
        services.AddSingleton<ITrackerService, TrackerService>();
        services.AddSingleton<ITorrentRepository, TorrentRepository>();
        services.AddSingleton<IIniciarTorrent, IniciarTorrent>();
        services.AddSingleton<ITorrentService, TorrentService>();

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

            // --- CACHE E ARQUIVOS ---
            AutoSaveLoadFastResume = settings.LoadFastResume,
            AutoSaveLoadMagnetLinkMetadata = settings.LoadMagnetLinkMetadata,
            AutoSaveLoadDhtCache = settings.LoadDhtCache,
            CacheDirectory = settings.PastaCache,
            UsePartialFiles = settings.ArquivoParcial, // Desativado para ajudar o VLC a ler o arquivo direto
            DiskCacheBytes = settings.CacheBytesEmDisco, // 50MB de RAM dedicada a cache

            // --- STREAMING E WEBSEEDS ---
            HttpStreamingPrefix = settings.StreamingPrefix,
            WebSeedConnectionTimeout = settings.ConexaoTimeout,
            WebSeedSpeedTrigger = 0 // Baixa de fontes HTTP e P2P simultaneamente se disponível

        };
    }
}

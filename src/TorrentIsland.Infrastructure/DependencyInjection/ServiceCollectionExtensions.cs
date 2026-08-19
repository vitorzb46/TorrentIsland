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
using TorrentIsland.Domain.Interfaces;
using TorrentIsland.Infrastructure.Configuration;
using TorrentIsland.Infrastructure.Logging;
using TorrentIsland.Infrastructure.MonoTorrent;

namespace TorrentIsland.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registra logging, configuração e os serviços de torrent (Application + Infrastructure).
    /// </summary>
    public static IServiceCollection AddTorrentIsland(this IServiceCollection services, IConfiguration configuration, int maxLogs = 10)
    {
        int portaLivre = ObterPortaLivre();
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
            var settingBuilder = GetSettingBuilder(portaLivre);

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

    private static EngineSettingsBuilder GetSettingBuilder(int portaLivre)
    {
        return new EngineSettingsBuilder
        {
            // --- REDE E CONEXÕES ---
            AllowPortForwarding = true,
            AllowLocalPeerDiscovery = true,
            DhtEndPoint = new IPEndPoint(IPAddress.Any, 0), // Porta UDP dinâmica para DHT
            ListenEndPoints = new Dictionary<string, IPEndPoint>
                {
                    { "ipv4", new IPEndPoint(IPAddress.Any, 0) }, // Porta TCP/UDP dinâmica para Peers
                    { "ipv6", new IPEndPoint(IPAddress.IPv6Any, 0) }
                },
            MaximumConnections = 200,

            // --- CACHE E ARQUIVOS ---
            AutoSaveLoadFastResume = true,
            AutoSaveLoadMagnetLinkMetadata = true,
            AutoSaveLoadDhtCache = true,
            UsePartialFiles = false, // Desativado para ajudar o VLC a ler o arquivo direto
            DiskCacheBytes = 50 * 1024 * 1024, // 50MB de RAM dedicada a cache

            // --- STREAMING E WEBSEEDS ---
            HttpStreamingPrefix = $"http://127.0.0.1:{portaLivre}/torrent-stream/",
            WebSeedConnectionTimeout = TimeSpan.FromSeconds(15),
            WebSeedSpeedTrigger = 0 // Baixa de fontes HTTP e P2P simultaneamente se disponível

        };
    }

    private static int ObterPortaLivre()
    {
        using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        socket.Bind(new IPEndPoint(IPAddress.Loopback, 0)); // '0' força o Windows a dar uma porta vazia
        return ((IPEndPoint)socket.LocalEndPoint!).Port;
    }
}

using System.Net;
using Microsoft.Extensions.Hosting;
using MonoTorrent.Client;
using MonoTorrent.Dht;
using MonoTorrent.PieceWriter;
using TorrentIsland.Application.Settings;
using TorrentIsland.Infrastructure.Logging;

namespace TorrentIsland.Infrastructure.MonoTorrent;

public class ClientEngineInitializer : IHostedService
{
    public ClientEngine? Engine;

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        var engineState = AppSettings.ArquivoEngineState;

        await Lock.File.WaitAsync(TimeSpan.FromSeconds(5), cancellationToken);
        try
        {
            if (File.Exists(engineState))
            {
                Engine = await Task.Run(async () => await ClientEngine.RestoreStateAsync(engineState));
                return;
            }
        }
        catch (Exception ex)
        {
            Log.Salvar($"[Engine] Falha ao restaurar de {engineState}: {ex.Message}. " +
                   $"Criando engine vazia.");
        }
        finally { Lock.File.Release(); }

        await Task.Run(() => Engine ??= NewClientEngine());
    }

    private static ClientEngine NewClientEngine()
    {
        var settings = GetSettingBuilder().ToSettings();
        var newEngine = new ClientEngine(settings);
        return newEngine;
    }

    public Task StopAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    private static EngineSettingsBuilder GetSettingBuilder()
    {
        var settings = new AppSettings();

        return new EngineSettingsBuilder
        {
            // --- REDE E CONEXÕES ---
            AllowPortForwarding = AppSettings.RedirecionarPorta,
            AllowLocalPeerDiscovery = AppSettings.DescobertaPeerLocal,
            DhtEndPoint = settings.IpV4, // Porta UDP dinâmica para DHT
            ListenEndPoints = new Dictionary<string, IPEndPoint>
                {
                    { "ipv4", settings.IpV4 }, // Porta TCP/UDP dinâmica para Peers
                    { "ipv6", settings.IpV6 }
                },
            MaximumConnections = AppSettings.EngineConnections,
            ConnectionRetryDelays = AppSettings.Retry,
            ConnectionTimeouts = AppSettings.PeerTimeout,
            DhtBootstrapRouters = DhtRouter(),

            // --- CACHE E ARQUIVOS ---
            AutoSaveLoadFastResume = AppSettings.LoadFastResume,
            AutoSaveLoadMagnetLinkMetadata = AppSettings.LoadMagnetLinkMetadata,
            AutoSaveLoadDhtCache = AppSettings.LoadDhtCache,
            CacheDirectory = AppSettings.CacheFolder,
            UsePartialFiles = AppSettings.ArquivoParcial, // Desativado para ajudar o VLC a ler o arquivo direto
            DiskCachePolicy = CachePolicy.ReadsAndWrites,
            DiskCacheBytes = AppSettings.CacheBytesEmDisco, // 150MB de RAM dedicada a cache

            // --- STREAMING E WEBSEEDS ---
            HttpStreamingPrefix = settings.StreamingPrefix,
            WebSeedConnectionTimeout = AppSettings.ConexaoTimeout,
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

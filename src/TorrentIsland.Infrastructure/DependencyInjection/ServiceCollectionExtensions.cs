using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MonoTorrent.Client;
using TorrentIsland.Application.Contracts;
using TorrentIsland.Application.Interfaces;
using TorrentIsland.Application.Medias.Commands;
using TorrentIsland.Application.Medias.Queries;
using TorrentIsland.Application.Services;
using TorrentIsland.Domain.Interfaces;
using TorrentIsland.Infrastructure.Configuration;
using TorrentIsland.Infrastructure.Events;
using TorrentIsland.Infrastructure.Helpers;
using TorrentIsland.Infrastructure.Interfaces;
using TorrentIsland.Infrastructure.Logging;
using TorrentIsland.Infrastructure.MonoTorrent;
using TorrentIsland.Infrastructure.Renderers;
using TorrentIsland.Infrastructure.Services;
using TorrentIsland.Infrastructure.VLC;

namespace TorrentIsland.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registra logging, configuração e os serviços de torrent (Application + Infrastructure).
    /// </summary>
    public static IServiceCollection AddTorrentIsland(this IServiceCollection services,
                                                      IConfiguration configuration,
                                                      IConsoleLogRenderer? renderer = null,
                                                      int maxLogs = 10,
                                                      ClientEngine? clientEngine = null,
                                                      VlcPlayerService? vlc = null)
    {
        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.AddRingBuffer(renderer, maxLogs);
            builder.AddFileLogger();
        });

        services.AddLocalization();
        services.AddSingleton(AppSettingsProvider.Carregar(configuration));

        if (clientEngine is null || vlc is null)
            throw new InvalidOperationException(
            "ClientEngine/vlc não foi inicializado. Instancie-os antes de configurar os serviços.");

        services.AddSingleton(clientEngine);
        services.AddSingleton(vlc);
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
        services.AddSingleton<ITorrentStatusEvent, TorrentStatusEvent>();
        services.AddSingleton<ITorrentLoopRenderer, TorrentLoopRenderer>();
        services.AddHostedService<TorrentLoopRenderer>();
        services.AddSingleton<IPlayerMonitorRenderer, PlayerMonitorRenderer>();
        services.AddSingleton<IDownloadQueueService, DownloadQueueService>();
        services.AddSingleton<IStreamService, StreamService>();
        services.AddSingleton<IFormattingHelper, FormattingHelper>();
        services.AddSingleton<IConsoleLogRenderer, ConsoleLogRenderer>();
        services.AddSingleton<ILogPainel, LogPainel>();

        var binaries = services.BuildServiceProvider().GetRequiredService<IDLService>();
        Task.Run(async () => await binaries.CheckBinariesAsync());

        return services;
    }
}

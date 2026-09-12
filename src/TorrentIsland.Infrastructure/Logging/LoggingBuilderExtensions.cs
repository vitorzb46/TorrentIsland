using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TorrentIsland.Application.Settings;
using TorrentIsland.Infrastructure.Interfaces;

namespace TorrentIsland.Infrastructure.Logging;

public static class LoggingBuilderExtensions
{
    /// <summary>
    /// Registra o buffer em anel como provider de log e como singleton — consumidores
    /// (renderer de console, sink WPF) resolvem a mesma instância usada pelo LoggerFactory.
    /// </summary>
    public static ILoggingBuilder AddRingBuffer(this ILoggingBuilder builder, IConsoleLogRenderer renderer, int maxLogs = 10)
    {
        var provider = new RingBufferLoggerProvider(renderer, maxLogs);
        builder.Services.AddSingleton(provider);
        builder.AddProvider(provider);
        return builder;
    }

    /// <summary>
    /// Registra a gravação em arquivo como provider de log.
    /// </summary>
    public static ILoggingBuilder AddFileLogger(this ILoggingBuilder builder, string? logPath = null)
    {
        var path = logPath ?? AppSettings.AppLog;
        var provider = new FileLogger(path);
        builder.Services.AddSingleton(provider);
        builder.AddProvider(provider);
        return builder;
    }
}

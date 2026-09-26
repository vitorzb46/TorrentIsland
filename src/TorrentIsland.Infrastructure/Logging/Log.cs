using Microsoft.Extensions.Logging;
using TorrentIsland.Application.Settings;

namespace TorrentIsland.Infrastructure.Logging;

public static class Log
{
    private static readonly ILoggerFactory Factory = LoggerFactory.Create(builder =>
    {
        builder.AddFileLogger(stackTrace: AppSettings.StackTrace);
    });

    public static void Salvar(string mensagem) =>
        Factory.CreateLogger("Player").LogInformation("{Mensagem}", mensagem);
}
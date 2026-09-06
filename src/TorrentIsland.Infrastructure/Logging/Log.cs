using Microsoft.Extensions.Logging;

namespace TorrentIsland.Infrastructure.Logging;

public static class Log
{
    private static readonly ILoggerFactory Factory = LoggerFactory.Create(builder => builder.AddFileLogger());

    public static void Salvar(string mensagem) =>
        Factory.CreateLogger("Player").LogInformation("{Mensagem}", mensagem);
}
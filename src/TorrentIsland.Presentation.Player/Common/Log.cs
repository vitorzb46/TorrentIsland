using TorrentIsland.Infrastructure.Logging;
using Microsoft.Extensions.Logging;
using TorrentIsland.Application.Settings;

namespace TorrentIsland.Presentation.Player.Common;

/// <summary>
/// Fachada de log do player: grava no <see cref="AppSettings.AppLog"/> reutilizando o FileLoggerProvider
/// compartilhado da TorrentIsland.Infrastructure.
/// </summary>
public static class Log
{
    private static readonly ILoggerFactory Factory = LoggerFactory.Create(builder =>
    {
        builder.AddFileLogger(stackTrace: AppSettings.StackTrace);
    });

    public static void Salvar(string mensagem) =>
        Factory.CreateLogger("Player").LogInformation("{Mensagem}", mensagem);
}

using TorrentIsland.Infrastructure.Logging;
using Microsoft.Extensions.Logging;

namespace TorrentIsland.Presentation.Player;

/// <summary>
/// Fachada de log do player: grava no player-debug.log reutilizando o FileLoggerProvider
/// compartilhado da TorrentIsland.Infrastructure.
/// </summary>
public static class Log
{
    private static readonly ILoggerFactory Factory = LoggerFactory.Create(builder => builder.AddFileLogger());

    public static void Salvar(string mensagem) =>
        Factory.CreateLogger("Player").LogInformation("{Mensagem}", mensagem);
}

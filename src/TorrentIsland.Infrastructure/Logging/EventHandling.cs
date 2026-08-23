using Microsoft.Extensions.Logging;
using Spectre.Console;
using TorrentIsland.Application.Interfaces;
using TorrentIsland.Infrastructure.Interfaces;
using TorrentIsland.Infrastructure.MonoTorrent;

namespace TorrentIsland.Infrastructure.Logging;

public class EventHandling(IManagers managers, ILogger<TorrentRepository> logger, CancellationToken token = default) : IEventHandling
{
    private IManagers Managers { get; } = managers;
    private ILogger<TorrentRepository> Logger { get; } = logger;

    public async Task EventsAsync(Guid id)
    {
        Console.WriteLine("Iniciando depêndencias.");

        if (!Managers.All.TryGetValue(id, out var manager))
        {
            Logger.LogWarning("Torrent ({TorrentId}) não encontrado.", id);
            return;
        }

        string Nome() => Markup.Escape(manager.Torrent?.Name ?? "Torrent desconhecido");

        manager.PeersFound += (o, e) =>
        {
            Logger.LogInformation("{Nome} -> {NovosPares} novos pares encontrados ({ParesExistentes} existentes).",
                Nome(), e.NewPeers, e.ExistingPeers);
        };

        manager.PeerConnected += async (o, e) =>
        {
            Logger.LogDebug("{Nome} -> Par conectado: {Peer} ({Direcao}).",
                Nome(), e.Peer, e.Direction);
        };

        manager.PeerDisconnected += async (o, e) =>
        {
            Logger.LogDebug("{Nome} -> Par desconectado: {Peer}.", Nome(), e.Peer);
        };

        manager.PieceHashed += async (o, e) =>
        {
            Logger.LogDebug("{Nome} -> Peça {PieceIndex} verificada - passou: {HashPassed} (progresso {Progresso:0.0}%).",
                Nome(), e.PieceIndex, e.HashPassed, e.Progress);
        };

        manager.ConnectionAttemptFailed += async (o, e) =>
        {
            Logger.LogDebug("{Nome} -> [yellow]Falha de conexão[/] com {Peer}: {Razao}.", Nome(), e.Peer, e.Reason);
        };

        manager.TorrentStateChanged += (o, e) =>
        {
            Logger.LogInformation("{Nome} -> Estado alterado: {Antigo} -> {Novo}",
                Nome(), e.OldState, e.NewState);
        };

        // Console
        AppDomain.CurrentDomain.ProcessExit += async (sender, e) =>
        {
            Logger.LogInformation("[red]Encerrando processo...[/]");

            await Task.Delay(2000, token).ConfigureAwait(false);
        };

        Console.CancelKeyPress += async (sender, e) =>
        {
            e.Cancel = true;

            Logger.LogInformation("[red]Ctrl + c pressionado. Aguarde...[/]");

            await Task.Delay(2000, token).ConfigureAwait(false);

            Environment.Exit(0);
        };
    }
}

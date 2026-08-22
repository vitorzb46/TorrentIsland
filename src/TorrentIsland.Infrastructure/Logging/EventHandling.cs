using Microsoft.Extensions.Logging;
using Spectre.Console;
using TorrentIsland.Application.Interfaces;
using TorrentIsland.Infrastructure.Interfaces;
using TorrentIsland.Infrastructure.MonoTorrent;

namespace TorrentIsland.Infrastructure.Logging;

public class EventHandling(IManagers managers, ILogger<TorrentRepository> logger) : IEventHandling
{
    private IManagers Managers { get; } = managers;
    private ILogger<TorrentRepository> Logger { get; } = logger;

    public async Task EventsAsync(Guid id)
    {
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
            await Task.Delay(500).ConfigureAwait(false);
        };

        manager.PeerDisconnected += async (o, e) =>
        {
            Logger.LogDebug("{Nome} -> Par desconectado: {Peer}.", Nome(), e.Peer);
            await Task.Delay(500).ConfigureAwait(false);
        };

        manager.PieceHashed += async (o, e) =>
        {
            Logger.LogDebug("{Nome} -> Peça {PieceIndex} verificada - passou: {HashPassed} (progresso {Progresso:0.0}%).",
                Nome(), e.PieceIndex, e.HashPassed, e.Progress);
            await Task.Delay(4000).ConfigureAwait(false);
        };

        manager.ConnectionAttemptFailed += async (o, e) =>
        {
            Logger.LogDebug("{Nome} -> [yellow]Falha de conexão[/] com {Peer}: {Razao}.", Nome(), e.Peer, e.Reason);
            await Task.Delay(2000).ConfigureAwait(false);
        };

        manager.TorrentStateChanged += (o, e) =>
        {
            Logger.LogInformation("{Nome} -> Estado alterado: {Antigo} -> {Novo}",
                Nome(), e.OldState, e.NewState);
        };
    }
}

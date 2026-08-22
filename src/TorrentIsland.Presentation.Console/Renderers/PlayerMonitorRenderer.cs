using Microsoft.Extensions.Logging;
using Spectre.Console;
using System.Diagnostics;
using TorrentIsland.Application.Interfaces;
using TorrentIsland.Domain.Enums;

namespace TorrentIsland.Presentation.Console.Renderers;

public class PlayerMonitorRenderer(ILogger<PlayerMonitorRenderer> logger) : IPlayerMonitorRenderer
{
    private readonly ILogger<PlayerMonitorRenderer> _logger = logger;

    public async Task MonitorPlayerAsync(
        Process? player,
        Func<IReadOnlyList<(Guid Id, string Nome, TorrentEstado Estado, int Seeds, int Peers)>> obterEstados,
        CancellationToken cancellationToken)
    {
        if (player is null)
        {
            _logger.LogWarning("Player não iniciado, pulando monitoramento");
            return;
        }

        _logger.LogInformation("Iniciando monitoramento do player...");

        int? ultimoSeed = null;
        while (!player.HasExited)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                RenderizarEstados(ref ultimoSeed, obterEstados);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao monitorar player");
            }

            await Task.Delay(1000, cancellationToken).ConfigureAwait(false);
        }

        _logger.LogInformation("Player encerrado, monitoramento finalizado.");
    }

    private void RenderizarEstados(
        ref int? ultimoSeed,
        Func<IReadOnlyList<(Guid Id, string Nome, TorrentEstado Estado, int Seeds, int Peers)>> obterEstados)
    {
        foreach (var (_, nome, estado, seeds, peers) in obterEstados())
        {
            if (ultimoSeed == null || seeds != ultimoSeed)
            {
                string nomeEscape = Markup.Escape(nome);
                _logger.LogInformation($"[[{nomeEscape}]] [cyan]{estado}[/] | seeds: {seeds} | peers: {peers}");
                ultimoSeed = seeds;
            }
        }
    }
}

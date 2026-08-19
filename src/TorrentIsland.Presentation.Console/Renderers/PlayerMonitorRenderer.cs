using Microsoft.Extensions.Logging;
using Spectre.Console;
using System.Diagnostics;
using TorrentIsland.Application.Interfaces;

namespace TorrentIsland.Presentation.Console.Renderers;

public class PlayerMonitorRenderer(ITorrentRepository repository, ILogger<PlayerMonitorRenderer> logger) : IPlayerMonitorRenderer
{
    private readonly ITorrentRepository _repository = repository;
    private readonly ILogger<PlayerMonitorRenderer> _logger = logger;

    public async Task MonitorPlayerAsync(Process? player, CancellationToken cancellationToken)
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
                RenderizarEstados(ref ultimoSeed);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao monitorar player");
            }

            await Task.Delay(1000, cancellationToken).ConfigureAwait(false);
        }

        _logger.LogInformation("Player encerrado, monitoramento finalizado.");
    }

    private void RenderizarEstados(ref int? ultimoSeed)
    {
        foreach (var (_, nome, estado, seeds, peers) in _repository.EstadoDosTorrents())
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

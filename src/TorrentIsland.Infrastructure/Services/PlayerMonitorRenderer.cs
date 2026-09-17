using Microsoft.Extensions.Logging;
using Spectre.Console;
using System.Diagnostics;
using TorrentIsland.Application.Interfaces;
using TorrentIsland.Domain.Enums;
using TorrentIsland.Infrastructure.Interfaces;

namespace TorrentIsland.Infrastructure.Services;

public class PlayerMonitorRenderer(ILogger<PlayerMonitorRenderer> logger, IConsoleLogRenderer renderer) : IPlayerMonitorRenderer
{
    private readonly ILogger<PlayerMonitorRenderer> _logger = logger;
    private readonly IConsoleLogRenderer renderer = renderer;

    public async Task MonitorPlayerAsync(
        Process? player,
        Func<IReadOnlyList<(Guid Id, string Nome, TorrentEstado Estado, int Seeds, int Peers)>> obterEstados,
        CancellationToken cancellationToken = default)
    {
        if (player is null)
        {
            _logger.LogWarning("Player não iniciado, pulando monitoramento");
            return;
        }

        _logger.LogInformation("Iniciando monitoramento do player...");

        System.Console.Clear();

        renderer.Painel.Limpar();

        renderer.Painel.Adicionar("Ctrl + c para encerrar o monitoramento.".PadRight(renderer.Largura));

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

        _logger.LogInformation("[red]Player encerrado, monitoramento finalizado.[/]");
    }

    private void RenderizarEstados(
        ref int? ultimoSeed,
        Func<IReadOnlyList<(Guid Id, string Nome, TorrentEstado Estado, int Seeds, int Peers)>> obterEstados)
    {
        foreach (var (_, nome, estado, seeds, peers) in obterEstados())
        {
            if (ultimoSeed == null || seeds != ultimoSeed)
            {
                _logger.LogInformation($"{Markup.Escape(nome)} - [cyan]{estado}[/] | seeds: {seeds} | peers: {peers}");
                ultimoSeed = seeds;
            }
        }
    }
}

using Microsoft.Extensions.Logging;
using System.Diagnostics;
using TorrentIsland.Application.Interfaces;

namespace TorrentIsland.Infrastructure.VLC;

public class PlayerLauncherService(ILogger<PlayerLauncherService> logger) : IPlayerLauncherService
{
    private readonly ILogger<PlayerLauncherService> _logger = logger;

    public Task<Process?> LaunchPlayerAsync(string url, CancellationToken cancellationToken = default)
    {
        var playerExe = Path.Combine(AppContext.BaseDirectory, "TorrentIsland.Presentation.Player.exe");
        if (!File.Exists(playerExe))
        {
            playerExe = Path.Combine(Directory.GetCurrentDirectory(), "TorrentIsland.Presentation.Player.exe");
        }

        if (!File.Exists(playerExe))
        {
            _logger.LogError("Player não encontrado: {PlayerExe}", playerExe);
            throw new FileNotFoundException($"Player não encontrado: {playerExe}");
        }

        try
        {
            var player = Process.Start(new ProcessStartInfo
            {
                FileName = playerExe,
                Arguments = $"\"{url}\"",
                UseShellExecute = true,
            });

            if (player != null)
            {
                player.EnableRaisingEvents = true;
                _logger.LogInformation("Player iniciado com sucesso: {PlayerExe}", playerExe);
            }

            return Task.FromResult(player);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao iniciar o player");
            throw;
        }
    }
}

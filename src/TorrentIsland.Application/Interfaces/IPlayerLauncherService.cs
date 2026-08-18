using System.Diagnostics;

namespace TorrentIsland.Application.Interfaces;

public interface IPlayerLauncherService
{
    Task<Process?> LaunchPlayerAsync(string url, CancellationToken cancellationToken);
}

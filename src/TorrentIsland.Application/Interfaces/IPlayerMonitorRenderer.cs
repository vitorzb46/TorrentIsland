using System.Diagnostics;

namespace TorrentIsland.Application.Interfaces
{
    public interface IPlayerMonitorRenderer
    {
        Task MonitorPlayerAsync(Process? player, CancellationToken cancellationToken);
    }
}
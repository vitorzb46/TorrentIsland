using System.Diagnostics;
using TorrentIsland.Domain.Enums;

namespace TorrentIsland.Application.Interfaces
{
    public interface IPlayerMonitorRenderer
    {
        Task MonitorPlayerAsync(
            Process? player,
            Func<IReadOnlyList<(Guid Id, string Nome, TorrentEstado Estado, int Seeds, int Peers)>> obterEstados,
            CancellationToken cancellationToken);
    }
}
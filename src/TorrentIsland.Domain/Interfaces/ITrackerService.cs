namespace TorrentIsland.Domain.Interfaces;

public interface ITrackerService
{
    Task<IList<string>> ObterListaAsync(CancellationToken cancellationToken = default);
}

namespace TorrentIsland.Domain.Interfaces;

public interface ITrackerService
{
    /// <summary>
    /// Obtém uma lista de trackers públicos via github para enriquecer os magnet links.
    /// </summary>
    /// <returns>Uma <see cref="IList"/> de strings representando a lista de trackers.</returns>
    Task<IList<string>> ObterListaAsync();
}

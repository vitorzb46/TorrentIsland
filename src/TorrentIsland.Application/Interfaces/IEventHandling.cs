namespace TorrentIsland.Application.Interfaces
{
    public interface IEventHandling
    {
        Task EventsAsync(Guid id);
    }
}
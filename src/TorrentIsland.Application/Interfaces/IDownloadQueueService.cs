namespace TorrentIsland.Application.Interfaces;

public interface IDownloadQueueService
{
    Guid StreamingTorrentId { get; }

    Task ClearStreaming();
    Task Enqueue(Guid torrentId);
    Task SetStreaming(Guid torrentId);
}

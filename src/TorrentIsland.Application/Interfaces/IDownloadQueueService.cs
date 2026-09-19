namespace TorrentIsland.Application.Interfaces;

public interface IDownloadQueueService
{
    Guid StreamingTorrentId { get; }

    Task ClearStreamingAsync();
    Task EnqueueAsync(Guid torrentId);
    Task SetStreamingAsync(Guid torrentId);
}

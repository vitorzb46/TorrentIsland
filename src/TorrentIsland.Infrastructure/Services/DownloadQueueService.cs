using TorrentIsland.Application.Interfaces;
using TorrentIsland.Infrastructure.Interfaces;
using TorrentIsland.Infrastructure.Logging;

namespace TorrentIsland.Infrastructure.Services;

public sealed class DownloadQueueService(IManagers manager) : IDownloadQueueService
{
    private readonly IManagers _manager = manager;
    private readonly Queue<Guid> _fila = new();
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public Guid StreamingTorrentId { get; private set; }

    public async Task SetStreaming(Guid torrentId)
    {
        await _semaphore.WaitAsync();
        try
        {
            StreamingTorrentId = torrentId;
            await Set(torrentId);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task ClearStreaming()
    {
        await _semaphore.WaitAsync();
        try
        {
            StreamingTorrentId = Guid.Empty;
            await TryNext();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task Enqueue(Guid torrentId)
    {
        await _semaphore.WaitAsync();
        try
        {
            _fila.Enqueue(torrentId);
            if (StreamingTorrentId != Guid.Empty)
                await Pause(torrentId);
            await TryNext();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async Task TryNext()
    {
        try
        {
            if (StreamingTorrentId != Guid.Empty) return;
    
            if (IsDownloading()) return;
    
            while (_fila.TryDequeue(out var id))
            {
                if (await _manager.TryStart(id))
                    return;
            }
        }
        catch (Exception ex)
        {
            Log.Salvar($"{ex.Message}");
        }
    }

    private bool IsDownloading() => _manager.HasTorrentActive(StreamingTorrentId);
    private async Task Pause(Guid id) => await _manager.PauseAsync(id);
    private async Task Set(Guid id) => await _manager.PauseAllExceptAsync(id);
}

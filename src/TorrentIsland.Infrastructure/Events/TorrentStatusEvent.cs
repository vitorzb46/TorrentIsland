using TorrentIsland.Application.DTOs;
using TorrentIsland.Application.Interfaces;
using TorrentIsland.Infrastructure.Interfaces;
using TorrentIsland.Infrastructure.Logging;

namespace TorrentIsland.Infrastructure.Events;

public class TorrentStatusEvent : ITorrentStatusEvent
{
    public static event EventHandler<TorrentDto>? TorrentUpdated;
    public static event EventHandler<TorrentDownloadDto>? TorrentQueue;
    private static readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly System.Timers.Timer _timer;
    private readonly IManagers _manager;
    private readonly IFormattingHelper _fb;
    private readonly IDownloadQueueService _downloadQueue;

    public TorrentStatusEvent(IManagers manager, IFormattingHelper fb, IDownloadQueueService downloadQueue)
    {
        _timer = new System.Timers.Timer(100);
        _timer.Elapsed += OnTimerElapsed;
        _manager = manager;
        _fb = fb;
        _downloadQueue = downloadQueue;
    }

    public void Start() => _timer.Start();

    public void Stop() => _timer.Stop();

    public System.Timers.Timer Timer() => _timer;

    public void Dispose()
    {
        _timer.Stop();
        _timer.Elapsed -= OnTimerElapsed;
        _timer.Dispose();
    }

    private async void OnTimerElapsed(object? sender, System.Timers.ElapsedEventArgs e)
    {
        if (!await _semaphore.WaitAsync(0).ConfigureAwait(false)) return;
        try
        {
            var torrents = await _manager.ObterTorrentsAsync();
            if (torrents.Count == 0) return;
            if (_downloadQueue.StreamingTorrentId != Guid.Empty
                && torrents.TryGetValue(_downloadQueue.StreamingTorrentId, out var streamDto))
            {
                TorrentUpdated?.Invoke(this, streamDto);

                if (streamDto.Progresso == 100.0)
                {
                    await _downloadQueue.ClearStreamingAsync();
                }
            }

            foreach (var kvp in torrents)
            {
                var id = kvp.Key;
                var dto = kvp.Value;

                var downloadDto = new TorrentDownloadDto
                {
                    TorrentId = id,
                    TorrentName = dto.Nome ?? "Desconhecido",
                    Status = dto.Estado.ToString(),
                    Progress = _fb.FormatarPorcentagem(dto.Progresso),
                    DownloadSpeed = _fb.FormatarBytes(dto.VelocidadeDownload),
                    UploadSpeed = _fb.FormatarBytes(dto.VelocidadeUpload),
                    Seeds = dto.Seeds,
                    Peers = dto.ParesDisponiveis,
                    TimeRemaining = dto.TempoEstimado!
                };

                TorrentQueue?.Invoke(this, downloadDto);
            }
        }
        catch (Exception ex)
        {
            Log.Salvar($"Erro no timer de status: {ex.Message}");
        }
        finally
        {
            _semaphore.Release();
        }
    }

}
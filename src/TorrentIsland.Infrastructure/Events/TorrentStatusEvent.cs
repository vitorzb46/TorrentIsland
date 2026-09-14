using TorrentIsland.Application.DTOs;
using TorrentIsland.Application.Interfaces;
using TorrentIsland.Infrastructure.Interfaces;
using TorrentIsland.Infrastructure.Logging;

namespace TorrentIsland.Infrastructure.Events;

public class TorrentStatusEvent : ITorrentStatusEvent
{
    public static event EventHandler<TorrentDto>? TorrentUpdated;
    private readonly System.Timers.Timer _timer;
    private readonly IManagers _manager;

    public TorrentStatusEvent(IManagers manager)
    {
        _timer = new System.Timers.Timer(500);
        _timer.Elapsed += OnTimerElapsed;
        Start();
        _manager = manager;
    }

    public void Start()
    {
        Log.Salvar($"{typeof(System.Timers.Timer)} iniciado!");
        _timer.Start();
    }

    public void Stop()
    {
        Log.Salvar($"{typeof(System.Timers.Timer)} parado!");
        _timer.Stop();
    }

    public System.Timers.Timer Timer()
    {
        return _timer;
    }

    public void Dispose()
    {
        _timer.Stop();
        _timer.Dispose();
    }

    private async void OnTimerElapsed(object? sender, System.Timers.ElapsedEventArgs e)
    {
        var torrents = await _manager.ObterTorrentsAsync();
        var id = torrents.Select(id => id.Key).FirstOrDefault();
        if (torrents.TryGetValue(id, out var dto))
        {
            Log.Salvar($"{typeof(TorrentDto)} enviado!");
            TorrentUpdated?.Invoke(this, dto);
        }
    }

}
using TorrentIsland.Application.DTOs;

namespace TorrentIsland.Application.Interfaces;

public interface ITorrentStatusEvent
{
    void Start();
    void Stop();
    System.Timers.Timer Timer();
    void Dispose();
}
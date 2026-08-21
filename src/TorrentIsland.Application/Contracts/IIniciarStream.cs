namespace TorrentIsland.Application.Contracts;

public interface IIniciarStream
{
    Task StartAsync(string magnet);
}

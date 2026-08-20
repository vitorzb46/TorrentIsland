namespace TorrentIsland.Application.Contracts
{
    public interface IIniciarTorrent
    {
        Task StartAsync(string input);
    }
}
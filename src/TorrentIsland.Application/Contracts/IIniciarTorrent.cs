namespace TorrentIsland.Application.Contracts
{
    public interface IIniciarTorrent
    {
        Task<Guid> StartAsync(string input);
    }
}
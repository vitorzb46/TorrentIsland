namespace TorrentIsland.Application.Interfaces;

public interface IStreamService
{
    Task<string> ToPlayerAsync(string caminhoOuUrl);
}


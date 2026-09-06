namespace TorrentIsland.Application.Interfaces;

public interface IDLService
{
    Task<string> GetStreamingUrl(string url);  
}

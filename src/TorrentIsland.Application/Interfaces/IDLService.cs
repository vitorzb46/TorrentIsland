namespace TorrentIsland.Application.Interfaces;

public interface IDLService
{
    Task CheckBinariesAsync();
    Task<string> GetStreamingUrl(string url);  
}

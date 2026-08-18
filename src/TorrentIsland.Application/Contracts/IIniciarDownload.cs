namespace TorrentIsland.Application.Contracts
{
    public interface IIniciarDownload
    {
        Task<Guid> DownloadAsync(string url);
        Task<Guid> PastaDownload(string filePath);
    }
}
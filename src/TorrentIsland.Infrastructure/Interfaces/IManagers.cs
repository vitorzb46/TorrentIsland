using MonoTorrent;
using MonoTorrent.Client;
using MonoTorrent.Streaming;
using System.Collections.Concurrent;
using TorrentIsland.Application.DTOs;
using TorrentIsland.Infrastructure.DTOs;

namespace TorrentIsland.Infrastructure.Interfaces
{
    public interface IManagers
    {
        ConcurrentDictionary<Guid, TorrentManager> All { get; set; }
        Task PauseAsync(Guid id);
        Task PauseAllExceptAsync(Guid id);
        bool HasTorrentActive(Guid streamingId);
        Task SaveEngine();
        Task<List<Guid>> AddTorrentsAsync();
        Task AguardarMetadata(TorrentManager manager);
        Task AguardarMetadata(IList<TorrentManager> managers);
        TorrentDadosBrutos CriarDadosBrutos(Guid id, TorrentManager manager);
        IEnumerable<string> GetTrackers(Guid id);
        Task<Torrent> LoadAsync(string path);
        Task<Torrent> LoadAsync(Memory<byte> data);
        Task<Torrent> LoadAsync(HttpClient client, Uri url, string savePath);
        Task<bool> TryStart(Guid id);
        Task<TorrentManager?> ObterManagerPorIdAsync(Guid id);
        Task<List<TorrentManager>> ObterManagersAsync();
        Task<IReadOnlyDictionary<Guid, TorrentDto>> ObterTorrentsAsync();
        MagnetLink Parse(string magnet);
        void RegistroId(Guid id, TorrentManager manager);
        Task StreamBuffer(TorrentManager manager);
        Task<IHttpStream> StreamHttp(TorrentManager manager, ITorrentManagerFile torrent, bool prebuffer = true);
        Task<IList<TorrentManager>> StreamingAsync(Torrent torrent, string savePath);
        Task<IList<TorrentManager>> StreamingAsync(MagnetLink magnet, string savePath);
        Task<IList<TorrentManager>> TorrentDownloadAsync(Torrent torrent, string savePath);
        Task<IList<TorrentManager>> TorrentDownloadAsync(MagnetLink magnet, string savePath);
    }
}
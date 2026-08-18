namespace TorrentIsland.Domain.Interfaces;

public interface IDownloadService
{
    Task<Guid> BaixarPorPastaAsync(string pastaTorrents, IProgress<double>? progress = null);
    Task<Guid> BaixarAsync(string magnet, IProgress<double>? progress = null);
    Task<Guid> StreamAsync(string magnet, IProgress<double>? progress = null);

    Task PausarAsync(Guid id);
    Task PararAsync(Guid id);
    Task RemoverAsync(Guid id);
    Task AnunciarDht(Guid id);
    Task AnunciarPeerLocal(Guid id);

    //Eventos
    event Action<string, double> ProgressoAtualizado;
}

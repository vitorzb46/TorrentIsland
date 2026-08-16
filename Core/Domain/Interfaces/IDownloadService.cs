namespace Domain.Interfaces;

public interface IDownloadService
{
    /// <summary>
    /// Baixa todos os arquivos torrents da pasta.
    /// </summary>
    /// <param name="pastaTorrents">Pasta onde estão os torrents.</param>
    /// <param name="progress">Barra de progresso.</param>
    /// <returns>Id único de um torrent.</returns>
    Task<Guid> BaixarPorPastaAsync(string pastaTorrents, IProgress<double>? progress = null);
    /// <summary>
    /// Baixa urls magnéticas.
    /// </summary>
    /// <param name="magnet">Url magnética.</param>
    /// <param name="progress">Barra de progresso.</param>
    /// <returns>Id único de um torrent.</returns>
    Task<Guid> BaixarAsync(string magnet, IProgress<double>? progress = null);
    /// <summary>
    /// Inicia o streaming de um magnet link, abrindo o Player WPF e monitorando os eventos
    /// do manager até o Player ser fechado.
    /// </summary>
    /// <param name="magnet">Url magnética.</param>
    /// <param name="progress">Barra de progresso.</param>
    /// <returns>Resultado do streaming (HttpPrefix + FullUri).</returns>
    Task<Stream> StreamAsync(string magnet, IProgress<double>? progress = null);

    //Eventos
    event Action<string, double> ProgressoAtualizado;
}

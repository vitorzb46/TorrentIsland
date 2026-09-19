using TorrentIsland.Application.Interfaces;
using TorrentIsland.Infrastructure.Interfaces;
using TorrentIsland.Infrastructure.Logging;

namespace TorrentIsland.Infrastructure.Services;
/// <summary>
/// Gerencia a fila de downloads, garantindo que apenas um torrent baixe por vez
/// e que o torrent em streaming tenha prioridade absoluta sobre os demais.
/// </summary>
/// <remarks>
/// A fila é FIFO (First-in, First-out). Quando um torrent é marcado como streaming via
/// <see cref="SetStreamingAsync"/>, todos os outros são pausados e a fila
/// fica bloqueada até que <see cref="ClearStreamingAsync"/> seja chamado.
/// Um timer de segurança reavalia a fila a cada 5 segundos para recuperar
/// o fluxo caso algum evento de transição seja perdido.
/// </remarks>
public sealed class DownloadQueueService : IDownloadQueueService
{
    private readonly IManagers _manager;
    private readonly Queue<Guid> _fila = new();
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public DownloadQueueService(IManagers manager)
    {
        _manager = manager;
        var _safetyTimer = new System.Timers.Timer(TimeSpan.FromSeconds(5).TotalMilliseconds);
        _safetyTimer.Elapsed += async (_,_) =>
        {
            await PumpQueueSafelyAsync().ConfigureAwait(false);
        };
        _safetyTimer.Start();
    }
    
    /// <summary>
    /// Identificador do torrent atualmente em streaming, ou <see cref="Guid.Empty"/>
    /// quando não há streaming ativo.
    /// </summary>
    /// <remarks>
    /// Enquanto este valor for diferente de <see cref="Guid.Empty"/>, a fila
    /// permanece bloqueada: nenhum outro torrent é iniciado.
    /// </remarks>
    public Guid StreamingTorrentId { get; private set; }

    /// <summary>
    /// Define o torrent informado como o streaming atual, pausando todos os demais.
    /// </summary>
    /// <param name="torrentId">Identificador do torrent que terá prioridade absoluta.</param>
    /// <remarks>
    /// Após esta chamada, a fila fica bloqueada até que <see cref="ClearStreamingAsync"/>
    /// seja invocado. Se já houver um streaming ativo, ele será substituído.
    /// </remarks>
    public async Task SetStreamingAsync(Guid torrentId)
    {
        await _semaphore.WaitAsync();
        try
        {
            StreamingTorrentId = torrentId;
            await SetAsync(torrentId);
        }
        finally
        {
            _semaphore.Release();
        }
    }
    
    /// <summary>
    /// Encerra o streaming atual e libera a fila para iniciar o próximo torrent.
    /// </summary>
    public async Task ClearStreamingAsync()
    {
        await _semaphore.WaitAsync();
        try
        {
            StreamingTorrentId = Guid.Empty;
            await TryNextFromQueueAsync();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Adiciona um torrent ao final da fila e tenta iniciar o próximo disponível.
    /// </summary>
    /// <param name="torrentId">Identificador do torrent a ser enfileirado.</param>
    public async Task EnqueueAsync(Guid torrentId)
    {
        await _semaphore.WaitAsync();
        try
        {
            _fila.Enqueue(torrentId);
            if (StreamingTorrentId != Guid.Empty)
                await PauseAsync(torrentId);
            await TryNextFromQueueAsync();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Tenta iniciar o próximo torrent elegível da fila, respeitando a
    /// prioridade do streaming.
    /// </summary>
    /// <remarks>
    /// O método é idempotente: se houver streaming ativo, se algum torrent
    /// da fila já estiver baixando, ou se o semáforo não puder ser adquirido
    /// imediatamente, ele retorna sem efeito. Torrents que recusarem o início
    /// (ex: removidos ou completos) são descartados e o próximo é tentado.
    /// </remarks>
    private async Task TryNextFromQueueAsync()
    {
        if (!await _semaphore.WaitAsync(0)) return;
        try
        {
            if (StreamingTorrentId != Guid.Empty) return;
    
            if (IsDownloading()) return;
    
            while (_fila.TryDequeue(out var id))
            {
                try
                {
                    if (await _manager.TryStart(id))
                        return;                   
                }
                catch (Exception ex)
                {
                    Log.Salvar($"[Fila] Erro ao iniciar {id}: {ex.Message}. Pulando.");
                }
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Verifica se existe algum torrent ativo (baixando ou semeando).
    /// </summary>
    /// <returns><c>true</c> se houver download em andamento; caso contrário, <c>false</c>.</returns>
    private bool IsDownloading() => _manager.HasTorrentActive(StreamingTorrentId);

    /// <summary>
    /// Pausa o torrent especificado.
    /// </summary>
    /// <param name="id">Identificador do torrent a ser pausado.</param>
    private async Task PauseAsync(Guid id) => await _manager.PauseAsync(id);

    /// <summary>
    /// Pausa todos os torrents, exceto o informado.
    /// </summary>
    /// <param name="id">Identificador do torrent que deve permanecer ativo.</param>
    private async Task SetAsync(Guid id) => await _manager.PauseAllExceptAsync(id);

    /// <summary>
    /// Executa o avanço da fila, capturando e logando
    /// qualquer exceção. Usado como safety net pelo timer periódico.
    /// </summary>
    /// <remarks>
    /// Este método é "best-effort": eventuais falhas são registradas em log sem propagação.
    /// Como <see cref="TryNextFromQueueAsync"/> é idempotente, chamá-lo com
    /// frequência é inofensivo.
    /// </remarks>
    private async Task PumpQueueSafelyAsync()
    {
        await Task.Run(async () =>
        {
            try {await TryNextFromQueueAsync().ConfigureAwait(false);}
            catch (Exception ex) { Log.Salvar($"[Fila] Safety net falhou: {ex.Message}"); }
        }).ConfigureAwait(false);
    }
}

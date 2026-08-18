using TorrentIsland.Domain.Interfaces;

namespace TorrentIsland.Application.Services;

/// <summary>
/// Obtém a lista de trackers públicos (best-effort) para enriquecer os magnet links.
/// </summary>
public sealed class TrackerService : ITrackerService
{
    private static readonly HttpClient Http = new()
    {
        Timeout = TimeSpan.FromSeconds(12)
    };

    public async Task<IList<string>> ObterListaAsync(CancellationToken cancellationToken = default)
    {
        const string url = "https://raw.githubusercontent.com/ngosang/trackerslist/master/trackers_best.txt";
        string trackers = string.Empty;
        try
        {
            trackers = await Http.GetStringAsync(url, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                Console.WriteLine("[[AVISO]] Tempo limite esgotado ao baixar a lista de trackers (Timeout). O download tentará iniciar apenas com os trackers originais.");
            }
            else
            {
                Console.WriteLine("[[ERRO]] Operação cancelada pelo usuário.");
                throw;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Falha ao obter lista de trackers: {ex.Message}");
        }

        return [.. trackers.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)];
    }
}

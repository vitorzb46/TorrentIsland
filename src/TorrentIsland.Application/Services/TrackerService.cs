using TorrentIsland.Domain.Interfaces;

namespace TorrentIsland.Application.Services;

public sealed class TrackerService : ITrackerService
{
    public async Task<IList<string>> ObterListaAsync()
    {
        HttpClient Http = new();
        using var ctsGitHub = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        const string url = "https://raw.githubusercontent.com/ngosang/trackerslist/master/trackers_best.txt";
        string trackers = string.Empty;
        try
        {
            trackers = await Http.GetStringAsync(url, ctsGitHub.Token).ConfigureAwait(false);
            if (trackers.Length == 0)
            {
                //fallback harded coded later
            }
        }
        catch (OperationCanceledException)
        {
            if (ctsGitHub.Token.IsCancellationRequested)
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

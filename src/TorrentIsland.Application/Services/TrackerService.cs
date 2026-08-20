using Microsoft.Extensions.Logging;
using TorrentIsland.Domain.Interfaces;

namespace TorrentIsland.Application.Services;

public sealed class TrackerService(ILogger<TrackerService> logger) : ITrackerService
{
    public ILogger<TrackerService> Logger { get; } = logger;

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
            if (!ctsGitHub.Token.IsCancellationRequested)
            {
                Logger.LogInformation("[yellow]Tempo limite esgotado ao baixar a lista de trackers (Timeout). O download tentará iniciar apenas com os trackers originais.[/]");
            }
            else
            {
                Logger.LogError("[red]Operação cancelada pelo usuário.[/]");
                throw;
            }
        }
        catch (Exception ex)
        {
            Logger.LogDebug($"Falha ao obter lista de trackers: {ex.Message}");
        }

        return [.. trackers.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)];
    }
}

using TorrentIsland.Application.Interfaces;
using TorrentIsland.Domain.Exceptions;

namespace TorrentIsland.Application.Services;

public class StreamService(ITorrentRepository repository) : IStreamService
{
    private readonly ITorrentRepository repository = repository;
    public async Task<string> ToPlayerAsync(string caminhoOuUrl)
    {
        var id = await repository.AddEngineAsync(caminhoOuUrl, true).ConfigureAwait(false);
        if (id.Count > 1) throw new InvalidManyManagerException();
        var stream = await repository.StartStreamAsync(id[0]).ConfigureAwait(false);
        return stream;
    }
}


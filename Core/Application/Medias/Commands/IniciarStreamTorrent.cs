using Core.Domain.Exceptions;
using Domain.Interfaces;

namespace Application.Medias.Commands;

public class IniciarStreamTorrent(IDownloadService downloadService) : IIniciarStreamTorrent
{
    public async Task<Stream> StreamAsync(string magnetLink)
    {
        if (string.IsNullOrWhiteSpace(magnetLink))
            throw new ParamaterException();

        if (magnetLink.StartsWith("magnet:"))
            throw new InvalidMagnetLinkException();

        return await downloadService.StreamAsync(magnetLink);
    }
}

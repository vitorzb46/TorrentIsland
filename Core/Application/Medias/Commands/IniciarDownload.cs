using Application.Contracts;
using Core.Domain.Exceptions;
using Domain.Interfaces;
using MonoTorrent;

namespace Application.Medias.Commands;

public class IniciarDownload(IDownloadService downloadService) : IIniciarDownload
{
    private readonly IDownloadService _downloadService = downloadService;
    public async Task<Guid> ExecutarPorPastaAsync(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ParamaterException();
            
        return await _downloadService.BaixarAsync(filePath);
    }
    public async Task<Guid> ExecutarAsync(string magnetLink)
    {
        if (string.IsNullOrWhiteSpace(magnetLink))
            throw new ParamaterException();

        if (magnetLink.StartsWith("magnet:"))
            throw new InvalidMagnetLinkException();

        return await _downloadService.BaixarAsync(magnetLink);
    }
}    

using TorrentIsland.Domain.Exceptions;

namespace TorrentIsland.Application.Validators;

public static class MagnetLinkValidator
{
    public static void Validar(string magnetLink)
    {
        if (string.IsNullOrWhiteSpace(magnetLink))
            throw new ParamaterException();

        if (!magnetLink.StartsWith("magnet:", StringComparison.OrdinalIgnoreCase))
            throw new InvalidMagnetLinkException();
    }
}

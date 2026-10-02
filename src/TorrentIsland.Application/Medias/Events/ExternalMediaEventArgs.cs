using TorrentIsland.Application.Medias.Enums;

namespace TorrentIsland.Application.Medias.Events;
public class ExternalMediaEventArgs(FileType type) : EventArgs
{
    public FileType Type { get; } = type;
}
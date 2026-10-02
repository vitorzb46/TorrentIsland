using TorrentIsland.Application.Medias.Enums;

namespace TorrentIsland.Application.Medias.Events;

public class PlaybackEventArgs(PlaybackType type) : EventArgs
{
    public PlaybackType Type { get; } = type;
}
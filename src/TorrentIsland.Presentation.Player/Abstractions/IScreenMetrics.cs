using TorrentIsland.Presentation.Player.Common;

namespace TorrentIsland.Presentation.Player.Abstractions;

public interface IScreenMetrics
{
    double PrimaryScreenWidth { get; }
    double PrimaryScreenHeight { get; }
    ScreenWorkArea WorkArea { get; }
}

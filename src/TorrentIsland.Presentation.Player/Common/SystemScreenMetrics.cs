using System.Windows;
using TorrentIsland.Presentation.Player.Abstractions;

namespace TorrentIsland.Presentation.Player.Common;

public readonly record struct ScreenWorkArea(double Left, double Top, double Bottom, double Width, double Height);

public sealed class SystemScreenMetrics : IScreenMetrics
{
    public double PrimaryScreenWidth => SystemParameters.PrimaryScreenWidth;
    public double PrimaryScreenHeight => SystemParameters.PrimaryScreenHeight;
    public ScreenWorkArea WorkArea => new(
        SystemParameters.WorkArea.Left,
        SystemParameters.WorkArea.Top,
        SystemParameters.WorkArea.Bottom,
        SystemParameters.WorkArea.Width,
        SystemParameters.WorkArea.Height);
}
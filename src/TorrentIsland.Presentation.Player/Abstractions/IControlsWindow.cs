using System.Windows;

namespace TorrentIsland.Presentation.Player.Abstractions;

public readonly record struct ControlsGeometry(double Left, double Top, double Width);

public interface IControlsWindow
{
    double Height { get; }
    Visibility Visibility { get; set; }
    void Apply(ControlsGeometry geometry);
}
using LibVLCSharp.WPF;
using System.Windows;
using TorrentIsland.Presentation.Player.ViewModel;

namespace TorrentIsland.Presentation.Player.Abstractions;

public interface IPlayerUiContext
{
    PlayerViewModel ViewModel { get; }
    VideoView PwVideoView { get; set; }
    Window Property { get; }
    void ShowControls();
    void InvalidateVisual();
    void ApplyBlackBackground();
}

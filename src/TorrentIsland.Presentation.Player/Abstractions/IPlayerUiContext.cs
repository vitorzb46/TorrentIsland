using TorrentIsland.Presentation.Player.ViewModel;

namespace TorrentIsland.Presentation.Player.Abstractions;

public interface IPlayerUiContext
{
    PlayerViewModel ViewModel { get; }
    void ShowControls();
    void InvalidateVisual();
    void ApplyBlackBackground();
}

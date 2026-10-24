using LibVLCSharp.WPF;
using System.Windows;
using System.Windows.Input;
using TorrentIsland.Presentation.Player.ViewModel;

namespace TorrentIsland.Presentation.Player.Abstractions;

public interface IPlayerUiContext
{
    PlayerViewModel ViewModel { get; }
    VideoView PwVideoView { get; set; }
    Window Property { get; }
    void ShowPreferencesWindow();
    void AbrirMenu();
    void FecharMenu();
    void ReiniciarTimerInatividade();
    void ShowControls();
    void InvalidateVisual();
    void ApplyBlackBackground();
    void ToggleFullscreen();
    Task<bool> ProcessPaste(KeyEventArgs e);
    Task<bool> OpenUrl(KeyEventArgs e, string? title);
}

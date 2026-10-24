using System.Windows;
using System.Windows.Input;
using TorrentIsland.Presentation.Player.Abstractions;
using TorrentIsland.Presentation.Player.ViewModel;
using TorrentIsland.Presentation.Player.Windows.Settings;

namespace TorrentIsland.Presentation.Player.Common;

public static class ShortcutRegistrations
{
    private static Preferences? _preferencesWindow;
    public static void RegisterPlayerShortcuts(this IShortcutManager manager, IPlayerUiContext window, PlayerViewModel vm)
    {        
        manager.Register(Key.F11, ModifierKeys.None, window.ToggleFullscreen);
        manager.Register(Key.Space, ModifierKeys.None, vm.TogglePlay);
        manager.Register(Key.Left, ModifierKeys.None, vm.RetrocederTempo);
        manager.Register(Key.Right, ModifierKeys.None, vm.AvancarTempo);
        manager.Register(Key.M, ModifierKeys.None, vm.ToggleMute);

        manager.Register(Key.J, ModifierKeys.None, () => vm.ExecuteAudioDelay(true, 0.5));
        manager.Register(Key.K, ModifierKeys.None, () => vm.ExecuteAudioDelay(false, 0.5));
        manager.Register(Key.Oem5, ModifierKeys.None, () => vm.ExecuteSubDelay(true, 0.5));
        manager.Register(Key.Oem6, ModifierKeys.None, () => vm.ExecuteSubDelay(false, 0.5));
        manager.Register(Key.Oem5, ModifierKeys.Control, () => vm.ExecuteSubDelay(true, 5.0));
        manager.Register(Key.Oem6, ModifierKeys.Control, () => vm.ExecuteSubDelay(false, 5.0));

        manager.Register(Key.T, ModifierKeys.Control | ModifierKeys.Shift, () => vm.ExecuteCommand(vm.OpenTorrentFileCommand));
        manager.Register(Key.S, ModifierKeys.Control | ModifierKeys.Shift, () => vm.ExecuteCommand(vm.StopPlayerCommand));
        manager.Register(Key.O, ModifierKeys.Control, () => vm.ExecuteCommand(vm.OpenVideoCommand));
        manager.Register(Key.P, ModifierKeys.Control, () => vm.ExecuteCommand(vm.OpenFolderCommand));
        manager.Register(Key.U, ModifierKeys.Control, () => vm.ExecuteCommand(vm.OpenSubtitleExternalCommand));
        manager.Register(Key.I, ModifierKeys.Control, () => vm.ExecuteCommand(vm.OpenAudioExternalCommand));

        manager.Register(Key.F12, ModifierKeys.None, () => vm.ExecuteCommand(vm.PrintScreenCommand));

        manager.Register(Key.Multiply, ModifierKeys.None, () =>
        {
            if (_preferencesWindow is { IsLoaded: true })
            {
                _preferencesWindow.Activate();
                return Task.FromResult(true);
            }

            _preferencesWindow = new Preferences
            {
                Owner = window as Window,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

            _preferencesWindow.Closed += (_, _) => _preferencesWindow = null;
            
            _preferencesWindow.Show();
            _preferencesWindow.Activate();

            return Task.FromResult(true);
        });

        manager.Register(Key.Down, ModifierKeys.None, () =>
        {
            vm.ToggleVolume(false);
            window.ShowControls();
            return Task.FromResult(true);
        });
        manager.Register(Key.Up, ModifierKeys.None, () =>
        {
            vm.ToggleVolume(true);
            window.ShowControls();
            return Task.FromResult(true);
        });
        manager.Register(Key.Escape, ModifierKeys.None, () =>
        {
            if (vm.IsFullscreen) window.ToggleFullscreen();
            else window.Property.WindowState = WindowState.Minimized;
            return Task.FromResult(true);
        });
        manager.Register(Key.T, ModifierKeys.Control, async (e) =>
        {
            return await window.OpenUrl(e, "Mídia Streaming");
        });
        manager.Register(Key.Y, ModifierKeys.Control, async (e) =>
        {
            return await window.OpenUrl(e, "Youtube");
        });
        manager.Register(Key.V, ModifierKeys.Control, window.ProcessPaste);
    }
}
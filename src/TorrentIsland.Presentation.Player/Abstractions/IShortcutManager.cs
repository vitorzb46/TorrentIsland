using System.Windows.Input;

namespace TorrentIsland.Presentation.Player.Abstractions;

public interface IShortcutManager
{
    void Register(Key key, ModifierKeys modifiers, Action action);
    void Register(Key key, ModifierKeys modifiers, Func<KeyEventArgs, Task<bool>> action);
    void Register(Key key, ModifierKeys modifiers, Func<Task> action);
    Task<bool> TryHandleAsync(KeyEventArgs e);
}

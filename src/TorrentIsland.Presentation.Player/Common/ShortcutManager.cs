using System.Windows.Input;
using TorrentIsland.Presentation.Player.Abstractions;

namespace TorrentIsland.Presentation.Player.Common;

public sealed class ShortcutManager : IShortcutManager
{
    private readonly Dictionary<(Key, ModifierKeys), Func<KeyEventArgs, Task<bool>>> _shortcuts = [];

    public void Register(Key key, ModifierKeys modifiers, Func<Task> action)
    {
        _shortcuts[(key, modifiers)] = async (e) => { await action(); return true; };
    }

    public void Register(Key key, ModifierKeys modifiers, Action action)
    {
        _shortcuts[(key, modifiers)] = (e) => { action(); return Task.FromResult(true); };
    }

    public void Register(Key key, ModifierKeys modifiers, Func<KeyEventArgs, Task<bool>> action)
    {
        _shortcuts[(key, modifiers)] = action;
    }

    public async Task<bool> TryHandleAsync(KeyEventArgs e)
    {
        var mods = Keyboard.Modifiers;
        if (_shortcuts.TryGetValue((e.Key, mods), out var action))
        {
            return await action(e);
        }
        return false;
    }
}
using System.Windows.Threading;
using TorrentIsland.Presentation.Player.Abstractions;

namespace TorrentIsland.Presentation.Player.Common;

internal sealed class UiDispatcher(Dispatcher dispatcher) : IUiDispatcher
{
    public void BeginInvoke(Action action, DispatcherPriority priority)
        => dispatcher.BeginInvoke(action, priority);

    public void Invoke(Action action) => dispatcher.Invoke(action);
}
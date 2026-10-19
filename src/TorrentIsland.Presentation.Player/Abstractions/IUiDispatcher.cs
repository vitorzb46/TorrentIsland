using System.Windows.Threading;

namespace TorrentIsland.Presentation.Player.Abstractions;

public interface IUiDispatcher
{
    void BeginInvoke(Action action, DispatcherPriority priority);
    void Invoke(Action action);
}

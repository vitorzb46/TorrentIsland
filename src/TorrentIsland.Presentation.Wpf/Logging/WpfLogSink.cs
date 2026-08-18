using System.Collections.ObjectModel;
using System.Windows;
using TorrentIsland.Infrastructure.Logging;

namespace TorrentIsland.Presentation.Wpf.Logging;

/// <summary>
/// Consome o buffer em anel e publica as mensagens na UI (ObservableCollection) via Dispatcher.
/// </summary>
public sealed class WpfLogSink
{
    private readonly RingBufferLoggerProvider _buffer;

    public ObservableCollection<string> Entries { get; } = [];

    public WpfLogSink(RingBufferLoggerProvider buffer)
    {
        _buffer = buffer;
        _buffer.Changed += OnChanged;
    }

    private void OnChanged()
    {
        System.Windows.Application.Current?.Dispatcher.BeginInvoke(() =>
        {
            var snapshot = _buffer.Snapshot();
            Entries.Clear();
            foreach (var entry in snapshot)
            {
                Entries.Add(entry);
            }
        });
    }
}

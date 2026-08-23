using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace TorrentIsland.Infrastructure.Logging;

/// <summary>
/// Provider de log que mantém as últimas mensagens em um buffer circular na memória
/// e notifica consumidores (renderer de console, sink WPF, etc.) via evento <see cref="Changed"/>.
/// </summary>
public sealed class RingBufferLoggerProvider : ILoggerProvider
{
    private readonly ConcurrentQueue<string> _entries = new();

    public event Action? Changed;

    public int MaxLogs { get; }

    public RingBufferLoggerProvider(int maxLogs = 10)
    {
        MaxLogs = Math.Max(1, maxLogs);
    }

    public ILogger CreateLogger(string categoryName) => new Logger(this);

    /// <summary>Cópia pontual das mensagens atualmente no buffer.</summary>
    public string[] Snapshot() => [.. _entries];

    internal void Enqueue(string message)
    {
        _entries.Enqueue(message);
        while (_entries.Count > MaxLogs)
        {
            _entries.TryDequeue(out _);
        }

        Changed?.Invoke();
    }

    public void Dispose()
    {
    }

    private sealed class Logger : ILogger
    {
        private readonly RingBufferLoggerProvider _provider;

        public Logger(RingBufferLoggerProvider provider)
        {
            _provider = provider;
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            var message = formatter(state, exception);
            if (exception is not null)
            {
                message = $"{message} :: {exception}";
            }

            _provider.Enqueue($"{DateTime.Now:HH:mm:ss.fff} {message}".PadRight(110));
        }
    }
}

using Microsoft.Extensions.Logging;

namespace TorrentIsland.Infrastructure.Logging;

/// <summary>
/// Provider de log que grava as mensagens em arquivo (best-effort), com timestamp e serialização por lock.
/// </summary>
public sealed class FileLogger : ILoggerProvider
{
    private readonly string _path;
    private readonly object _sync = new();

    public FileLogger(string logPath)
    {
        _path = logPath;
    }

    public ILogger CreateLogger(string categoryName) => new Logger(this);

    internal void Write(string message)
    {
        try
        {
            lock (_sync)
            {
                File.AppendAllText(_path, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}{Environment.NewLine}");
            }
        }
        catch
        {
            // Ignora falhas de escrita em log (best-effort).
        }
    }

    public void Dispose()
    {
    }

    private sealed class Logger : ILogger
    {
        private readonly FileLogger _provider;

        public Logger(FileLogger provider)
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

            _provider.Write(message);
        }
    }
}

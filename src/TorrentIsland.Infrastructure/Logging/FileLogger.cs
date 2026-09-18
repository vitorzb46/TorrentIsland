using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace TorrentIsland.Infrastructure.Logging;

/// <summary>
/// Provider de log que grava as mensagens em arquivo (best-effort), com timestamp e serialização por lock.
/// </summary>
public sealed class FileLogger(string logPath, bool trace) : ILoggerProvider
{
    private readonly string _path = logPath;
    private readonly bool _trace = trace;
    private readonly object _sync = new();

    public ILogger CreateLogger(string categoryName) => new Logger(this);

    internal void Write(string message)
    {
        try
        {
            string mensagemFinal = message;

            if (_trace)
            {
                var stackTrace = new StackTrace(9, true); 
                var frame = stackTrace.GetFrame(0);
                var metodo = frame?.GetMethod();
                
                string quemChamou = metodo != null 
                    ? $"{metodo.DeclaringType?.Name}.{metodo.Name}" 
                    : "Desconhecido";

                mensagemFinal = $"{message} (Chamado por: {quemChamou})";
            }

            lock (_sync)
            {
                File.AppendAllText(_path, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {mensagemFinal}{Environment.NewLine}");
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

    private sealed class Logger(FileLogger provider) : ILogger
    {
        private readonly FileLogger _provider = provider;

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

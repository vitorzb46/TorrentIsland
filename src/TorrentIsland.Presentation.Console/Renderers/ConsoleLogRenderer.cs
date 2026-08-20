using Microsoft.Extensions.Hosting;
using Spectre.Console;
using System.Text;
using TorrentIsland.Infrastructure.Logging;

namespace TorrentIsland.Presentation.Console.Renderers;

/// <summary>
/// Renderiza no console o painel estático + as últimas linhas do buffer em anel.
/// Serializado por lock e com coalescência de ticks
/// </summary>
public sealed class ConsoleLogRenderer : BackgroundService
{
    private const int Largura = 110;
    private readonly RingBufferLoggerProvider _buffer;
    private readonly object _sync = new();
    private string _cliAtual = string.Empty;
    private DateTime _ultimaRenderizacao = DateTime.MinValue;
    private static readonly TimeSpan IntervaloMinimo = TimeSpan.FromMilliseconds(250);

    public LogPainel Painel { get; } = new();

    public ConsoleLogRenderer(RingBufferLoggerProvider buffer)
    {
        _buffer = buffer;
        _buffer.Changed += OnBufferChanged;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        stoppingToken.Register(() => _buffer.Changed -= OnBufferChanged);
        Render();
        return Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken);
    }

    private void OnBufferChanged() => Render();

    private void Render()
    {
        lock (_sync)
        {
            var agora = DateTime.UtcNow;
            if (agora - _ultimaRenderizacao < IntervaloMinimo)
            {
                return;
            }

            _ultimaRenderizacao = agora;

            var logs = _buffer.Snapshot();

            var sb = new StringBuilder();
            sb.Append(Painel.Conteudo());
            sb.AppendLine();
            sb.Append($"[cyan]{Multi(Largura - 1, '-')}[/]");
            sb.AppendLine();
            sb.Append($"{Multi(30, ' ')}[cyan]=== ÚLTIMOS LOGS DO SISTEMA ===[/]".PadRight(Largura));
            sb.AppendLine();

            foreach (var log in logs.Reverse().Take(_buffer.MaxLogs))
            {
                var textoLimpo = RemoverTodaFormatacao(log);

                // Trunca se necessário
                var linha = textoLimpo.Length > Largura - 1
                    ? textoLimpo.Substring(0, Largura - 3) + "..."
                    : textoLimpo.PadRight(Largura - 1);

                sb.Append($" {linha}\n");
            }

            int vazias = _buffer.MaxLogs - Math.Min(logs.Length, _buffer.MaxLogs);
            for (int i = 0; i < vazias; i++)
            {
                sb.AppendLine(new string(' ', Largura - 7));
            }

            var saida = sb.ToString();

            // Apenas atualiza se mudou
            if (saida != _cliAtual)
            {
                System.Console.SetCursorPosition(0, 0);
                AnsiConsole.Markup(saida);
                _cliAtual = saida;
            }
        }
    }

    public string Multi(int vezes = 0, char c = '\t')
    {
        return $"{new string(c, vezes)}";
    }
    private static string RemoverTodaFormatacao(string texto)
    {
        // Remove tags Spectre ([color], [/], etc)
        var semTags = System.Text.RegularExpressions.Regex.Replace(texto, @"\[/?[a-z]+\]", "");

        // Remove códigos ANSI
        var semAnsi = System.Text.RegularExpressions.Regex.Replace(
            semTags,
            @"\x1b\[[0-9;]*[mK]",
            string.Empty
        );

        return semAnsi;
    }

    internal object Multi(object numero, object c)
    {
        throw new NotImplementedException();
    }
}

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Spectre.Console;
using System.Text;
using TorrentIsland.Infrastructure.Interfaces;
using TorrentIsland.Infrastructure.Logging;

namespace TorrentIsland.Presentation.Console.Renderers;

/// <summary>
/// Renderiza no console o painel estático + as últimas linhas do buffer em anel.
/// Serializado por lock e com coalescência de ticks
/// </summary>
public sealed class ConsoleLogRenderer : BackgroundService, IConsoleLogRenderer
{
    public int Vazio = ObterLargura() / 4;
    public int Largura { get; set; } = ObterLargura();

    private static int tempoRender = 100;
    private readonly IServiceProvider _services;
    private readonly object _sync = new();
    private string _cliAtual = string.Empty;
    private DateTime _ultimaRenderizacao = DateTime.MinValue;
    private static readonly TimeSpan IntervaloMinimo = TimeSpan.FromMilliseconds(tempoRender);

    private RingBufferLoggerProvider? _buffer;

    public ILogPainel Painel { get; }
    public int TempoRender { get => tempoRender; }

    public ConsoleLogRenderer(IServiceProvider services, ILogPainel painel)
    {
        _services = services;
        Painel = painel;
    }

    private RingBufferLoggerProvider Buffer =>
        _buffer ??= _services.GetRequiredService<RingBufferLoggerProvider>();

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Buffer.Changed += OnBufferChanged;
        stoppingToken.Register(() => Buffer.Changed -= OnBufferChanged);
        Render();
        return Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken);
    }

    private void OnBufferChanged() => Render();

    private void Render()
    {
        lock (_sync)
        {
            AtualizarDimensoes();
            var agora = DateTime.UtcNow;
            if (agora - _ultimaRenderizacao < IntervaloMinimo)
            {
                return;
            }

            _ultimaRenderizacao = agora;

            var logs = Buffer.Snapshot();

            var sb = new StringBuilder();
            sb.Append(Painel.Conteudo());
            sb.AppendLine();
            sb.Append($"[cyan]{Multi(Largura - 1, '-')}[/]");
            sb.AppendLine();
            sb.Append($"{Multi(Vazio, ' ')}[cyan]=== ÚLTIMOS LOGS DO SISTEMA ===[/]".PadRight(Largura));
            sb.AppendLine();

            foreach (var log in logs.Reverse().Take(Buffer.MaxLogs))
            {
                var textoLimpo = RemoverTodaFormatacao(log);

                // Trunca se necessário
                var linha = textoLimpo.Length > Largura - 1
                    ? string.Concat(textoLimpo.AsSpan(0, Largura - 3), "...")
                    : textoLimpo.PadRight(Largura - 1);

                sb.Append($" {linha}\n");
            }

            int vazias = Buffer.MaxLogs - Math.Min(logs.Length, Buffer.MaxLogs);
            for (int i = 0; i < vazias; i++)
            {
                sb.AppendLine(new string(' ', Largura - 1));
            }

            var saida = sb.ToString();

            // Apenas atualiza se mudou
            if (saida != _cliAtual)
            {
                System.Console.SetCursorPosition(0, 0);

                try
                {
                    AnsiConsole.Markup(saida);
                }
                catch (InvalidOperationException)
                {
                    AnsiConsole.Markup(Markup.Escape(saida));
                }

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
        //var semTags = System.Text.RegularExpressions.Regex.Replace(texto, @"\[/?[a-z]+\]", "");

        // Remove códigos ANSI
        var semAnsi = System.Text.RegularExpressions.Regex.Replace(
            texto,
            @"\x1b\[[0-9;]*[mK]",
            string.Empty
        );

        return semAnsi;
    }

    static int ObterLargura()
    {
        try
        {
            // Console Windows
            if (!System.Console.IsOutputRedirected)
            {
                return System.Console.WindowWidth;
            }

        }
        catch (Exception)
        {
            string? envColumns = Environment.GetEnvironmentVariable("COLUMNS");
            if (int.TryParse(envColumns, out int columns))
            {
                return columns;
            }
        }

        return 110;
    }

    private void AtualizarDimensoes()
    {
        Largura = ObterLargura();
        Vazio = Largura / 4;
    }
}

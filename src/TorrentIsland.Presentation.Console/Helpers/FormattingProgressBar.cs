using Spectre.Console;
using System.Globalization;
using System.Text;

namespace TorrentIsland.Presentation.Console.Helpers
{
    internal class FormattingProgressBar : IProgress<double>, IDisposable
    {
        private readonly string _title;
        private readonly int _largura;
        private readonly object _sync = new();
        private int _lastPercent = -1;
        private bool _disposed;
        /// <summary>
        /// Classe responsável por formatar barra de progresso no console.
        /// </summary>
        public FormattingProgressBar(string title = "Download", int largura = 30)
        {
            _title = string.IsNullOrWhiteSpace(title) ? "Download" : title.Trim();
            _largura = Math.Max(10, largura);
        }
        // Satisfaz a interface IProgress
        public void Report(double value)
        {
            Report(value, null);
        }

        public void Report(double value, StringBuilder? sb)
        {
            if (_disposed) return;

            var porcentagem = Math.Clamp(value, 0d, 1d) * 100d;
            var rounded = (int)Math.Round(porcentagem);

            lock (_sync)
            {
                if (sb == null)
                {
                    if (rounded == _lastPercent) return;

                    _lastPercent = rounded;
                    Render(rounded, null);
                    return;
                }

                _lastPercent = rounded;
                Render(rounded, sb);
            }
        }
        private void Render(int porcentagem, StringBuilder? sb)
        {
            var preenchido = (int)Math.Round(_largura * (porcentagem / 100d));
            preenchido = Math.Clamp(preenchido, 0, _largura);
            var vazio = _largura - preenchido;

            var cor = porcentagem switch
            {
                >= 70 => "cyan",
                >= 30 => "yellow",
                _ => "red"
            };

            if (sb == null)
            {

                AnsiConsole.MarkupLine($" {_title}: [[[{cor}]{new string('-', preenchido)}{new string(' ', vazio)}[/]]]");

                if (porcentagem >= 100)
                {
                    System.Console.WriteLine();
                }
                return;
            }
            else
            {
                sb.Append(CultureInfo.InvariantCulture, $" {_title}: [[[{cor}]{new string('-', preenchido)}{new string(' ', vazio)}[/]]]");

                if (porcentagem >= 100)
                {
                    sb.AppendLine();
                }
            }
        }
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
        }
    }
}

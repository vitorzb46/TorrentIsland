using System.Text;
using TorrentIsland.Presentation.Console.Helpers;

namespace TorrentIsland.Presentation.Console.Renderers
{
    public static class ProgressBarRenderer
    {
        public static void BarraDeProgresso(IProgress<double>? progress, StringBuilder sb, double progresso, string titulo)
        {
            if (progress is null)
            {
                using (var temp = new FormattingProgressBar(titulo))
                {
                    temp.Report(progresso, sb);
                }
                sb.AppendLine();
            }
            else if (progress is FormattingProgressBar consoleBar)
            {
                consoleBar.Report(progresso, sb);
                sb.AppendLine();
            }
        }
    }
}

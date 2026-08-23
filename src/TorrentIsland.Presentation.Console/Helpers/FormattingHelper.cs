using System.Globalization;
using TorrentIsland.Domain.Enums;

namespace TorrentIsland.Presentation.Console.Helpers
{
    public class FormattingHelper : IFormattingHelper
    {
        public string FormatarBytes(long bytes)
        {
            return bytes switch
            {
                >= 1_073_741_824 => $"{bytes / 1_073_741_824:0.00} GB",
                >= 1_048_576 => $"{bytes / 1_048_576:0.0} MB",
                >= 1024 => $"{bytes / 1024:0} KB",
                _ => $"{bytes:0} B",
            };
        }
        public string TempoEstimado(long TamanhoTotal, long BytesRecebidos, double VelocidadeDownload, TorrentEstado Estado, double Progresso)
        {
            long bytesRestantes = TamanhoTotal - BytesRecebidos;
            double velocidade = VelocidadeDownload;
            double etaSegundos = velocidade > 0 ? bytesRestantes / velocidade : double.PositiveInfinity;

            string etaTexto;
            if (Estado is TorrentEstado.Semeando or TorrentEstado.Concluido || Progresso >= 100.0)
            {
                etaTexto = "Concluído ";
            }
            else if (double.IsPositiveInfinity(etaSegundos))
            {
                etaTexto = Estado == TorrentEstado.Baixando ? "Buscando peers... " : "Parado ";
            }
            else
            {
                TimeSpan etaTimeSpan = TimeSpan.FromSeconds(etaSegundos);
                etaTexto = etaTimeSpan.Days > 0
                    ? etaTimeSpan.ToString(@"d\.hh\:mm\:ss", CultureInfo.CurrentCulture)
                    : etaTimeSpan.ToString(@"hh\:mm\:ss", CultureInfo.CurrentCulture);
            }

            return etaTexto;
        }
    }
}

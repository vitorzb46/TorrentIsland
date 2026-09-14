using System.Globalization;
using TorrentIsland.Application.Interfaces;
using TorrentIsland.Domain.Enums;

namespace TorrentIsland.Presentation.Console.Helpers
{
    public class FormattingHelper : IFormattingHelper
    {
        public double FormatarPorcentagem(double progresso)
        {
            double d = Math.Clamp(progresso, 0.0, 100.0);
            return Math.Round(d, 2, MidpointRounding.AwayFromZero);
        }
        public string FormatarBytes(long bytes)
        {
            string[] sufixos = ["B/s", "KB/s", "MB/s", "GB/s", "TB/s"];
            int ordem = 0;
            while (bytes >= 1024 && ordem < sufixos.Length - 1)
            {
                ordem++;
                bytes /= 1024;
            }
            return $"{bytes:0.00} {sufixos[ordem]}";
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

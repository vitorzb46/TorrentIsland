using TorrentIsland.Domain.Enums;

namespace TorrentIsland.Application.Interfaces
{
    public interface IFormattingHelper
    {
        double FormatarPorcentagem(double progresso);
        string FormatarBytes(long bytes);
        string TempoEstimado(long TamanhoTotal, long BytesRecebidos, double VelocidadeDownload, TorrentEstado Estado, double Progresso);
    }
}
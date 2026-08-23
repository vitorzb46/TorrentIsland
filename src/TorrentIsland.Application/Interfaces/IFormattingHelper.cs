using TorrentIsland.Domain.Enums;

namespace TorrentIsland.Presentation.Console.Helpers
{
    public interface IFormattingHelper
    {
        string FormatarBytes(long bytes);
        string TempoEstimado(long TamanhoTotal, long BytesRecebidos, double VelocidadeDownload, TorrentEstado Estado, double Progresso);
    }
}
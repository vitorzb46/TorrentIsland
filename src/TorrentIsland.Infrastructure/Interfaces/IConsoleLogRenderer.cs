namespace TorrentIsland.Infrastructure.Interfaces;

public interface IConsoleLogRenderer
{
    int Largura { get; }

    int TempoRender { get; }

    ILogPainel Painel { get; }

    string Multi(int vezes = 0, char c = '\t');
}
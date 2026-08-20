namespace TorrentIsland.Domain.Interfaces;

public interface ITorrentLoopRenderer
{
    Task TorrentInfoRender(IProgress<double>? progress = null);
}

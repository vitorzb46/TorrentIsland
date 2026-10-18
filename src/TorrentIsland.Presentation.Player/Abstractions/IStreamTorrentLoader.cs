namespace TorrentIsland.Presentation.Player.Abstractions;

public interface IStreamTorrentLoader
{
    Task LoadAsync(string filePathOrUrl);
}

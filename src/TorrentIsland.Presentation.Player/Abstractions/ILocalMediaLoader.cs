namespace TorrentIsland.Presentation.Player.Abstractions;

public interface ILocalMediaLoader
{
    Task LoadAsync(string filePathOrUrl);
}

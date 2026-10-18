namespace TorrentIsland.Presentation.Player.Abstractions;

public interface IMediasFolderLoader
{
    Task LoadAsync(List<string>? folder);
}

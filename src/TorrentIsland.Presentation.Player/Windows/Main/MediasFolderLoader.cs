using TorrentIsland.Presentation.Player.Abstractions;

namespace TorrentIsland.Presentation.Player.Windows.Main;


public class MediasFolderLoader(IPlayerUiContext context, MediaFactory mediaFactory) : IMediasFolderLoader
{
    private readonly IPlayerUiContext _context = context;
    private readonly MediaFactory _mediaFactory = mediaFactory;

    public async Task LoadAsync(List<string>? folder)
    {
        ArgumentNullException.ThrowIfNull(folder);

        foreach (string file in folder)
        {
            bool v = await TryAddMediaAsync(file);
            if (!v) continue;
        }

        await _context.ViewModel.PlayNextMedia();
    }

    private async Task<bool> TryAddMediaAsync(string file)
    {
        var factory = await _mediaFactory.CreateAsync(file);
        if (factory?.Media == null) return false;

        _context.ViewModel.AddMedia(factory.Media);
        return true;
    }
}
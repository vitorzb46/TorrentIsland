using System.Windows;
using TorrentIsland.Application.Interfaces;
using TorrentIsland.Application.Settings;
using TorrentIsland.Infrastructure.Logging;
using TorrentIsland.Presentation.Player.Abstractions;

namespace TorrentIsland.Presentation.Player.Windows.Main;


public class StreamTorrentLoader(IPlayerUiContext context, IStreamService stream, ILocalMediaLoader local) : IStreamTorrentLoader
{
    private readonly IPlayerUiContext _context = context;
    private readonly IStreamService _stream = stream;
    private readonly ILocalMediaLoader _local = local;

    public async Task LoadAsync(string filePathOrUrl)
    {
        try
        {
            await WaitForStreamService(filePathOrUrl);
        }
        catch (Exception ex)
        {
            InvalidStream(ex);
        }
    }

    private async Task WaitForStreamService(string url)
    {
        await _context.ViewModel.LoadingStateAsync("Iniciando streaming...", async () =>
        {
            string streamUrl = await GetStreamUrl(url);
            AppSettings.OneStream = true;
            await _local.LoadAsync(streamUrl);
        });
    }

    private async Task<string> GetStreamUrl(string url)
    {
        return await Task.Run(async () =>
        {
            return await _stream.ToPlayerAsync(url);
        });
    }

    private void InvalidStream(Exception ex)
    {
        Log.Salvar($"Erro ao iniciar stream de torrent: {ex.Message}");
        MessageBox.Show($"Não foi possível iniciar o stream do torrent!", "Player",
            MessageBoxButton.OK, MessageBoxImage.Error);

        if (_context.ViewModel.GetMediaPlayer != null)
            _context.ViewModel.IsVideoVisible = true;
    }

}
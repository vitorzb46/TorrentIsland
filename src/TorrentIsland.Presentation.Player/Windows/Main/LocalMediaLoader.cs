using System.Windows;
using TorrentIsland.Infrastructure.Logging;
using TorrentIsland.Presentation.Player.Abstractions;
using TorrentIsland.Presentation.Player.Common;

namespace TorrentIsland.Presentation.Player.Windows.Main;


public class LocalMediaLoader(IPlayerUiContext context, MediaFactory factory) : ILocalMediaLoader
{
    private readonly IPlayerUiContext _context = context;
    private readonly MediaFactory _factory = factory;

    public async Task LoadAsync(string filePathOrUrl)
    {
        try
        {
            await InitializeMediaAsync(filePathOrUrl);
        }
        catch (Exception ex)
        {
            InvalidMedia(ex, filePathOrUrl);
        }
    }

    private async Task InitializeMediaAsync(string url)
    {
        await _context.ViewModel.LoadingStateAsync("Carregando mídia...", async () =>
        {
            try
            {
                await PrepareForNewMediaAsync();
                await MediaTimestamp.Load();
    
                var factory = await _factory.CreateAsync(url);
                if (factory?.Media is null) return;
    
                _context.ViewModel.FilePath = factory.FilePath;
                Log.Salvar(_context.ViewModel.FilePath);
                var timeCached = await MediaTimestamp.LoadCache(factory.FilePath);
                var media = factory.Media;
                _context.ViewModel.SetMedia(media, timeCached.Time);
    
                await Utils.AtualizarUIAsync(async () =>
                {
                    await _context.ViewModel.PopulateTracksAsync();
                    _context.ViewModel.SetPause(false);
                    _context.ShowControls();
                    _context.InvalidateVisual();
                    _context.ApplyBlackBackground();
                    await Task.Delay(300); //Tempo de espera para evitar artefato visual
                });
            }
            catch (Exception ex)
            {
                Log.Salvar($"{ex.Message}\n{ex.StackTrace}");
            }
        });
    }

    private async Task PrepareForNewMediaAsync()
    {
        var media = _context.ViewModel.GetMedia();
        if (media is null) return;
        await _context.ViewModel.SaveCacheAsync();
        _context.ViewModel.MediaDispose();
    }

    private static void InvalidMedia(Exception ex, string url)
    {
        Log.Salvar($"Erro ao carregar mídia: {ex.Message} {ex.StackTrace} {ex.InnerException}");
        MessageBox.Show($"Não foi possível carregar a mídia: {url}", "Player",
            MessageBoxButton.OK, MessageBoxImage.Error);
    }

}
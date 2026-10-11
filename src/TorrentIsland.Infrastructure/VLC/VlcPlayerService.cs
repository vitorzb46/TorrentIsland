using LibVLCSharp.Shared;
using TorrentIsland.Infrastructure.Logging;

namespace TorrentIsland.Infrastructure.VLC;

/// <summary>
/// Wrapper para LibVLCSharp: encapsula o ciclo de vida de LibVLC/MediaPlayer.
/// A integração com a UI (VideoView) fica na camada de apresentação.
/// </summary>
public sealed class VlcPlayerService
{
    private readonly LibVLC _libVLC;
    private MediaPlayer _mediaPlayer;
    private Media? _media;
    private bool _disposed;

    public VlcPlayerService()
    {
        Core.Initialize();
        _libVLC = new LibVLC(VlcConfiguration.ToArgs());
        _mediaPlayer = new MediaPlayer(_libVLC)
        {
            EnableMouseInput = false,
            EnableKeyInput = false            
        };
    }

    public LibVLC LibVLC => _libVLC;
    public MediaPlayer MediaPlayer => _mediaPlayer;

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        try
        {
            _mediaPlayer?.Stop();
            _mediaPlayer?.Dispose();

            _media?.Dispose();
            _media = null;

            _libVLC?.Dispose();
        }
        catch (Exception ex)
        {
            Log.Salvar($"Erro dispose VlcPlayerService: {ex}");
        }
    }
}

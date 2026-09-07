using LibVLCSharp.Shared;

namespace TorrentIsland.Infrastructure.VLC;

/// <summary>
/// Wrapper para LibVLCSharp: encapsula o ciclo de vida de LibVLC/MediaPlayer.
/// A integração com a UI (VideoView) fica na camada de apresentação.
/// </summary>
public sealed class VlcPlayerService
{
    private readonly LibVLC _libVLC;
    private readonly MediaPlayer _mediaPlayer;
    private Media? _media;

    public VlcPlayerService()
    {
        Core.Initialize();
        _libVLC = new LibVLC();
        _mediaPlayer = new MediaPlayer(_libVLC)
        {
            EnableMouseInput = false
        };
    }

    public LibVLC LibVLC => _libVLC;
    public MediaPlayer MediaPlayer => _mediaPlayer;

    public void Dispose()
    {
        _media?.Dispose();
        _media = null;
        _mediaPlayer.Dispose();
        _libVLC.Dispose();
    }
}

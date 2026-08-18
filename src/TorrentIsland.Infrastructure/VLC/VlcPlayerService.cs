using LibVLCSharp.Shared;
using TorrentIsland.Domain.Interfaces;

namespace TorrentIsland.Infrastructure.VLC;

/// <summary>
/// Wrapper para LibVLCSharp: encapsula o ciclo de vida de LibVLC/MediaPlayer.
/// A integração com a UI (VideoView) fica na camada de apresentação.
/// </summary>
public sealed class VlcPlayerService : IPlayerService
{
    private readonly LibVLC _libVLC;
    private readonly MediaPlayer _mediaPlayer;
    private Media? _media;

    public VlcPlayerService()
    {
        Core.Initialize();
        _libVLC = new LibVLC();
        _mediaPlayer = new MediaPlayer(_libVLC);
    }

    public LibVLC LibVLC => _libVLC;
    public MediaPlayer MediaPlayer => _mediaPlayer;

    public bool EstaReproduzindo => _mediaPlayer.IsPlaying;

    public void Reproduzir(Uri uri)
    {
        _media?.Dispose();
        _media = new Media(_libVLC, uri);
        _mediaPlayer.Play(_media);
    }

    public void Pausar()
    {
        if (_mediaPlayer.IsPlaying)
        {
            _mediaPlayer.Pause();
        }
        else
        {
            _mediaPlayer.Play();
        }
    }

    public void Parar() => _mediaPlayer.Stop();

    public void Dispose()
    {
        _media?.Dispose();
        _media = null;
        _mediaPlayer.Dispose();
        _libVLC.Dispose();
    }
}

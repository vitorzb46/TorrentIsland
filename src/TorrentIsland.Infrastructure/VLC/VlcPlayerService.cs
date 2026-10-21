using LibVLCSharp.Shared;
using TorrentIsland.Application.Settings;
using TorrentIsland.Infrastructure.Logging;

namespace TorrentIsland.Infrastructure.VLC;

/// <summary>
/// Wrapper para LibVLCSharp: encapsula o ciclo de vida de LibVLC/MediaPlayer.
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

        _libVLC.Log += OnVlcLog;
    }
    
    public LibVLC LibVLC => _libVLC;
    public MediaPlayer MediaPlayer => _mediaPlayer;

    private void OnVlcLog(object? sender, LogEventArgs e)
    {
        if (!AppSettings.VlcVerboseLog) return;
        Log.Salvar($"[{e.Level}] {e.Module}: {e.Message}");
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _libVLC.Log -= OnVlcLog;
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
            Log.Salvar($"[Erro] dispose VlcPlayerService: {ex.Message}");
        }
    }
}

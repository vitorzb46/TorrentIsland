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

        var options = new string[]
        {
            // Rede
            "--network-caching=5000",       // Buffer para rede
            "--file-caching=5000",           // Buffer arquivos
            "--live-caching=3000",          // Buffer para live streams
            "--http-reconnect",             // Reconexão automática HTTP
            "--directx-3buffering",         // Buffer para DirectX
            "--direct3d11-hw-blending",

            // Decodificação
            "--avcodec-hw=any",             // Aceleração de hardware
            "--avcodec-skiploopfilter=all", // Pular filtro de loop

            "--clock-jitter=0",             // Nitidez
            "--swscale-mode=10",
            "--clock-synchro=0",
            "--drop-late-frames",           // Descartar frames atrasados
            "--skip-frames",                // Pular frames para manter sincronia
            "--no-video-title-show"
        };

        _libVLC = new LibVLC(options);
        _mediaPlayer = new MediaPlayer(_libVLC)
        {
            EnableMouseInput = false
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

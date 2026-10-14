using TorrentIsland.Application.Settings;

namespace TorrentIsland.Infrastructure.VLC;

public sealed class VlcConfiguration
{
    public static string[] ToArgs()
    {
        var args = new List<string>();

        if (AppSettings.VlcVerboseLog) args.Add("--verbose=2");
        
        // Rede
        if (AppSettings.VlcHttpReconnect) args.Add("--http-reconnect");
        args.Add($"--network-caching={AppSettings.VlcNetworkCachingMs}");
        args.Add($"--file-caching={AppSettings.VlcFileCachingMs}");
        args.Add($"--live-caching={AppSettings.VlcLiveCachingMs}");
        if (AppSettings.VlcDirect3DBuffering) args.Add("--directx-3buffering");
        if (AppSettings.VlcDirect3D11HwBlending) args.Add("--direct3d11-hw-blending");

        // Decodificação
        args.Add($"--avcodec-hw={AppSettings.VlcHardwareDecoding}");
        args.Add($"--avcodec-skiploopfilter={AppSettings.VlcSkipLoopFilter}");

        // Sincronia e escala
        args.Add($"--clock-jitter={AppSettings.VlcClockJitterMs}");
        args.Add($"--swscale-mode={AppSettings.VlcSwScaleMode}");
        args.Add($"--clock-synchro={AppSettings.VlcClockSynchro}");

        // Framerate
        if (AppSettings.VlcDropLateFrames) args.Add("--drop-late-frames");
        if (AppSettings.VlcSkipFrames) args.Add("--skip-frames");
        if (AppSettings.VlcNoVideoTitleShow) args.Add("--no-video-title-show");

        if (AppSettings.VlcDeinterlacing)
        {
            args.Add("--deinterlace=1");
            args.Add("--video-filter=deinterlace");
            args.Add($"--deinterlace-mode={AppSettings.VlcDeinterlaceMode}");
        }

        if (AppSettings.VlcSharpenFilter)
        {
            args.Add("--video-filter=sharpen");
            args.Add($"--sharpen-sigma={AppSettings.VlcSharpenSigma}"); // entre 0 e 2
        }
        
        return [.. args];
    }
}
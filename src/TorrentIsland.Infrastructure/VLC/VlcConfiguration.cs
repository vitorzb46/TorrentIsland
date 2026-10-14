using System.Globalization;
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

        var videoFilters = new List<string>();

        // Filtros técnicos vem primeiro na ordem de aplicação que os filtros estéticos
        if (AppSettings.VlcDeinterlacing && AppSettings.VlcDeinterlaceMode is string dm)
        {
            videoFilters.Add("deinterlace");
            args.Add($"--deinterlace-mode={dm}");
        }

        if (AppSettings.VlcSharpenFilter && AppSettings.VlcSharpenSigma is float sigma)
        {
            videoFilters.Add("sharpen");
            args.Add($"--sharpen-sigma={sigma.ToString(CultureInfo.InvariantCulture)}"); // entre 0 e 2
        }

        if (AppSettings.VlcGradfunFilter
            && AppSettings.VlcGradfunRadius is int radius
            && AppSettings.VlcGradfunStrength is float strength)
        {
            videoFilters.Add("gradfun");
            args.Add($"--gradfun-radius={radius}");
            args.Add($"--gradfun-strength={strength.ToString(CultureInfo.InvariantCulture)}");
        }

        //Estético
        if (AppSettings.VlcSepiaFilter)
        {
            videoFilters.Add("sepia");
            args.Add($"--sepia-intensity={AppSettings.VlcSepiaIntensity}");
        }

        if (videoFilters.Count > 0) args.Add($"--video-filter={string.Join(",", videoFilters)}");
        
        return [.. args];
    }
}
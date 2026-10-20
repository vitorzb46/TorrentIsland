using System.Windows.Input;
using TorrentIsland.Application.Medias.Enums;
using TorrentIsland.Application.Medias.Events;
using TorrentIsland.Application.Settings;
using TorrentIsland.Presentation.Player.Common;
using TorrentIsland.Presentation.Player.TvCast;

namespace TorrentIsland.Presentation.Player.ViewModel;

public partial class PlayerViewModel
{
    private ICommand? _openVideoCommand;
    public ICommand OpenVideoCommand =>
        _openVideoCommand ??= new RelayCommand(
            () => OpenFileExternalRequested?.Invoke(this, new ExternalMediaEventArgs(FileType.Video)));

    private ICommand? _openAudioExternalCommand;
    public ICommand OpenAudioExternalCommand =>
        _openAudioExternalCommand ??= new RelayCommand(
            () => OpenFileExternalRequested?.Invoke(this, new ExternalMediaEventArgs(FileType.Audio)));

    private ICommand? _openSubtitleExternalCommand;
    public ICommand OpenSubtitleExternalCommand =>
        _openSubtitleExternalCommand ??= new RelayCommand(
            () => OpenFileExternalRequested?.Invoke(this, new ExternalMediaEventArgs(FileType.Subtitle)));

    private ICommand? _openTorrentFileCommand;
    public ICommand OpenTorrentFileCommand =>
        _openTorrentFileCommand ??= new RelayCommand(
            () => OpenFileExternalRequested?.Invoke(this, new ExternalMediaEventArgs(FileType.Torrent)));

    private ICommand? _openFolderCommand;
    public ICommand OpenFolderCommand =>
        _openFolderCommand ??= new RelayCommand(
            () => OpenFileExternalRequested?.Invoke(this, new ExternalMediaEventArgs(FileType.Folder)));

    private ICommand? _toggleFullScreenCommand;
    public ICommand ToggleFullscreenCommand =>
        _toggleFullScreenCommand ??= new RelayCommand(
            () => PlaybackActionRequested?.Invoke(this, new PlaybackEventArgs(PlaybackType.Fullscreen)));

    private ICommand? _stopPlayerCommand;
    public ICommand StopPlayerCommand =>
        _stopPlayerCommand ??= new RelayCommand(
            () => PlaybackActionRequested?.Invoke(this, new PlaybackEventArgs(PlaybackType.Stop)));

    private ICommand? _torrentSearchCommand;
    public ICommand TorrentSearchCommand =>
        _torrentSearchCommand ??= new RelayCommand(
            () => TorrentSearchRequested?.Invoke(this, EventArgs.Empty));

    private ICommand? _torrentQueueCommand;
    public ICommand TorrentQueueCommand =>
        _torrentQueueCommand ??= new RelayCommand(
            () => TorrentQueueRequested?.Invoke(this, EventArgs.Empty));

    private ICommand? _openUrlYtbCommand;
    public ICommand OpenUrlYtbCommand =>
        _openUrlYtbCommand ??= new RelayCommand(
            () => OpenUrlStreamRequested?.Invoke("Youtube", EventArgs.Empty));

    private ICommand? _openUrlCommand;
    public ICommand OpenUrlCommand =>
        _openUrlCommand ??= new RelayCommand(
            () => OpenUrlStreamRequested?.Invoke("Media", EventArgs.Empty));

    private ICommand? _selectSubtitleCommand;
    public ICommand SelectSubtitleCommand =>
        _selectSubtitleCommand ??= new RelayCommand<int>(SelectSubtitleTrack);

    private ICommand? _selectAudioCommand;
    public ICommand SelectAudioCommand =>
        _selectAudioCommand ??= new RelayCommand<int>(SelectAudioTrack);

    private ICommand? _delayAudioCommand;
    public ICommand DelayAudioCommand =>
        _delayAudioCommand ??= new RelayCommand<(bool delay, double timeDelay)>(
            args =>
            {
                ToggleDelayAudio(args.delay, args.timeDelay);
                OsdMessageChanged?.Invoke(this, EventArgs.Empty);
            });

    private ICommand? _delaySpuCommand;
    public ICommand DelaySpuCommand =>
        _delaySpuCommand ??= new RelayCommand<(bool delay, double timeDelay)>(
            args =>
            {
                ToggleDelaySpu(args.delay, args.timeDelay);
                OsdMessageChanged?.Invoke(this, EventArgs.Empty);
            });

    private ICommand? _togglePlayCommand;
    public ICommand TogglePlayCommand =>
        _togglePlayCommand ??= new RelayCommand(TogglePlay);

    private ICommand? _toggleMuteCommand;
    public ICommand ToggleMuteCommand =>
        _toggleMuteCommand ??= new RelayCommand(ToggleMute);

    private ICommand? _setDeviceCommand;
    public ICommand SetDeviceCommand =>
        _setDeviceCommand ??= new RelayCommand<CastDevice>(SetTvCast);

    private ICommand? _stopCastingCommand;
    public ICommand StopCastingCommand =>
        _stopCastingCommand ??= new RelayCommand(StopTvCast);

    private ICommand? _hardwareDecodeCommand;
    public ICommand HardwareDecodeCommand =>
        _hardwareDecodeCommand ??= new RelayCommand<int>(SetHardwareDecoding);

    private ICommand? _skipLoopFilterCommand;
    public ICommand SkipLoopFilterCommand =>
        _skipLoopFilterCommand ??= new RelayCommand<int>(SetSkipLoop);

    private ICommand? _swScaleCommand;
    public ICommand SwScaleCommand =>
        _swScaleCommand ??= new RelayCommand<int>(SetSwScale);

    private ICommand? _clockSynchroCommand;
    public ICommand ClockSynchroCommand =>
        _clockSynchroCommand ??= new RelayCommand<int>(SetClockSynchro);

    private ICommand? _deinterlaceCommand;
    public ICommand DeinterlaceCommand =>
        _deinterlaceCommand ??= new RelayCommand<int>(SetDeinterlaceMode);

    private ICommand? _sharpenCommand;
    public ICommand SharpenCommand =>
        _sharpenCommand ??= new RelayCommand<float>(SetSharpen);

    private ICommand? _sepiaCommand;
    public ICommand SepiaCommand =>
        _sepiaCommand ??= new RelayCommand<int>(SetSepia);

    private ICommand? _gradfunCommand;
    public ICommand GradfunCommand =>
        _gradfunCommand ??= new RelayCommand<(int? radius, float? str)>((args) =>
        {
            SetGradfun(args.radius, args.str);
        });

    public static void SetGradfun(int? radius, float? strength)
    {
        if (radius is -1 && strength is -1f)
        {
            AppSettings.VlcGradfunRadius = null;
            AppSettings.VlcGradfunStrength = null;
            AppSettings.VlcGradfunFilter = false;
            return;
        }

        bool radiusIsValid = radius is >= 4 and <= 32;
        bool strengthIsValid = strength is >= 0.51f and <= 255.0f;
        AppSettings.VlcGradfunRadius = radiusIsValid ? radius : 16;
        AppSettings.VlcGradfunStrength = strengthIsValid ? strength : 1.2f;
        AppSettings.VlcGradfunFilter = radiusIsValid && strengthIsValid;
    }

    public static void SetSepia(int sepia)
    {
        bool isValid = sepia is >= 0 and <= 255;
        AppSettings.VlcSepiaIntensity = isValid ? sepia : 0;
        AppSettings.VlcSepiaFilter = isValid;
    }

    public static void SetSharpen(float sharpen)
    {
        if (sharpen == -1f)
        {
            AppSettings.VlcSharpenSigma = null;
            return;
        }

        bool isValid = sharpen is >= 0.0f and <= 2.0f;
        AppSettings.VlcSharpenFilter = isValid;
        AppSettings.VlcSharpenSigma = isValid ? sharpen : 0.05f;
    }

    public static void SetDeinterlaceMode(int dm)
    {
        AppSettings.VlcDeinterlaceMode = dm switch
        {
            1 => "discard",
            2 => "blend",
            3 => "mean",
            4 => "bob",
            5 => "linear",
            6 => "x",
            7 => "yadif",
            8 => "yadif2x",
            9 => "phosphor",
            10 => "ivtc",
            _ => null
        };

        AppSettings.VlcDeinterlacing = AppSettings.VlcDeinterlaceMode != null;
    }

    public static void SetClockSynchro(int cs)
    {
        if (cs is >= -1 and <= 1)
            AppSettings.VlcClockSynchro = cs;
    }

    public static void SetSwScale(int sw)
    {
        if (sw is >= 0 and <= 10)
            AppSettings.VlcSwScaleMode = sw;
    }

    public static void SetSkipLoop(int sl)
    {
        if (sl is >= 0 and <= 4)
            AppSettings.VlcSkipLoopFilter = sl;
    }

    public static void SetHardwareDecoding(int hw)
    {
        AppSettings.VlcHardwareDecoding = hw switch
        {
            0 => "any",
            1 => "dxva2",
            2 => "d3d11va",
            3 => "vaapi",
            4 => "vdpau_avcodec",
            5 => "cuvid",
            6 => "cuda",
            7 => "qsv",
            8 => "none",
            _ => "any"
        };
    }

    public static int CurrentHardwareDecodingIndex() =>
        AppSettings.VlcHardwareDecoding switch
        {
            "dxva2" => 1,
            "d3d11va" => 2,
            "vaapi" => 3,
            "vdpau_avcodec" => 4,
            "cuvid" => 5,
            "cuda" => 6,
            "qsv" => 7,
            "none" => 8,
            _ => 0 // Any - Automático
        };

    public void SetTvCast(CastDevice? device)
    {
        if (!_mediaCastManager.CastTo(device))
        {
            string v = device?.Name ?? "aparelho desconhecido";
            OsdMessage = $"Falha ao transmitir para {v}";
        }
    }

    public void StopTvCast() => _mediaCastManager.StopCasting();

    public void SelectAudioTrack(int trackId) => _mediaPlayer.SetAudioTrack(trackId);

    public void SelectSubtitleTrack(int spuId) => _mediaPlayer.SetSpu(spuId);

    public void TogglePlay()
    {
        if (_mediaPlayer.IsPlaying)
            _mediaPlayer.Pause();
        else
            _mediaPlayer.Play();
    }

    public void ToggleMute()
    {
        _mediaPlayer.Mute = !_mediaPlayer.Mute;
        IsMuted = _mediaPlayer.Mute;
    }

    public void ExecuteAudioDelay(bool advance, double value) =>
        DelayAudioCommand.Execute((advance, value));

    public void ExecuteSubDelay(bool advance, double value) =>
        DelaySpuCommand.Execute((advance, value));

    public void ExecuteCommand(ICommand cmd)
    {
        if (cmd.CanExecute(null))
            cmd.Execute(null);
    }
}

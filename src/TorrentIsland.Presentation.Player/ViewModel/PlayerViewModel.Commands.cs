using System.Windows.Input;
using TorrentIsland.Application.Medias.Enums;
using TorrentIsland.Application.Medias.Events;
using TorrentIsland.Presentation.Player.Common;

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

    private ICommand? _selectSubtitleCommand;
    public ICommand SelectSubtitleCommand =>
        _selectSubtitleCommand ??= new RelayCommand<int>(SelectSubtitleTrack);

    private ICommand? _selectAudioCommand;
    public ICommand SelectAudioCommand =>
        _selectAudioCommand ??= new RelayCommand<int>(SelectAudioTrack);

    private ICommand? _delaySpuCommand;
    public ICommand DelaySpuCommand =>
        _delaySpuCommand ??= new RelayCommand<(bool delay, double timeDelay)>(
            args =>
            {
                ToggleDelaySpu(args.delay, args.timeDelay);
                SubtitleDelayChanged?.Invoke(this, EventArgs.Empty);
            });

    private ICommand? _togglePlayCommand;
    public ICommand TogglePlayCommand =>
        _togglePlayCommand ??= new RelayCommand(TogglePlay);

    private ICommand? _toggleMuteCommand;
    public ICommand ToggleMuteCommand =>
        _toggleMuteCommand ??= new RelayCommand(ToggleMute);



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
}

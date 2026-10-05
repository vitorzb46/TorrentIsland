using LibVLCSharp.Shared;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using TorrentIsland.Application.Interfaces;
using TorrentIsland.Application.Medias.Events;
using TorrentIsland.Infrastructure.Events;
using TorrentIsland.Infrastructure.Logging;
using TorrentIsland.Presentation.Player.Common;

namespace TorrentIsland.Presentation.Player.ViewModel;

public sealed partial class PlayerViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly MediaPlayer _mediaPlayer;

    private bool _disposed;

    public LibVLC LibVLC { get; }

    #region Constructor
    public PlayerViewModel(LibVLC libVLC, MediaPlayer mediaPlayer, IFormattingHelper fb)
    {
        LibVLC = libVLC;
        _mediaPlayer = mediaPlayer;
        _mediaPlayer.SetAdjustFloat(VideoAdjustOption.Enable, 1.0f);
        _fb = fb;
        _mediaPlayer.PositionChanged += OnPositionChanged;
        _mediaPlayer.Playing += OnPlaying;
        _mediaPlayer.Paused += OnPaused;
        _mediaPlayer.Muted += OnMute;
        _mediaPlayer.Unmuted += OnMute;
        _mediaPlayer.Stopped += OnStopped;
        _mediaPlayer.EndReached += OnEndReached;
        _mediaPlayer.Buffering += OnPlayerBuffering;
        _mediaPlayer.MediaChanged += OnMediaChanged;
        _mediaPlayer.EncounteredError += OnEncounteredError;
        _mediaPlayer.LengthChanged += OnLengthChanged;
        Loading.LoadingMessageChanged += OnLoadingMessageChanged;
        TorrentStatusEvent.TorrentUpdated += OnTorrentUpdated;
        TorrentStatusEvent.TorrentQueue += OnTorrentQueue;
        LoadingDebounce();
    }
    #endregion

    public async Task LoadingStateAsync(string message, Func<Task> func)
    {
        await Utils.AtualizarUIAsync(async () =>
        {
            IsVideoVisible = false;
            IsOpening = true;
            IsLoading = true;
            LoadingMessage = message;
        });

        try
        {
            await func();
        }
        finally
        {
            await Utils.AtualizarUIAsync(async () =>
            {
                IsOpening = false;
                IsLoading = false;
                LoadingMessage = string.Empty;
                IsVideoVisible = true;
            });
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _mediaPlayer.PositionChanged -= OnPositionChanged;
        _mediaPlayer.Playing -= OnPlaying;
        _mediaPlayer.Paused -= OnPaused;
        _mediaPlayer.Muted -= OnMute;
        _mediaPlayer.Unmuted -= OnMute;
        _mediaPlayer.Stopped -= OnStopped;
        _mediaPlayer.EndReached -= OnEndReached;
        _mediaPlayer.Buffering -= OnPlayerBuffering;
        _mediaPlayer.MediaChanged -= OnMediaChanged;
        _mediaPlayer.EncounteredError -= OnEncounteredError;
        _mediaPlayer.LengthChanged -= OnLengthChanged;
        Loading.LoadingMessageChanged -= OnLoadingMessageChanged;
        TorrentStatusEvent.TorrentUpdated -= OnTorrentUpdated;
        TorrentStatusEvent.TorrentQueue -= OnTorrentQueue;

        try
        {
            if (_mediaPlayer.Media != null)
            {
                MediaTime = _mediaPlayer.Time;
                _ = MediaTimestamp.SaveCache(FilePath, MediaTime);
            }
        }
        catch (Exception ex)
        {
            Log.Salvar($"Não foi possível salvar o tempo do MediaTime: {ex.Message}");
        }
    }
}
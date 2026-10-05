using LibVLCSharp.Shared;
using System.IO;
using System.Text.RegularExpressions;
using TorrentIsland.Presentation.Player.Common;

namespace TorrentIsland.Presentation.Player.ViewModel;

public partial class PlayerViewModel
{
    [GeneratedRegex(@".*?(?:s\d+e\d+|\d+x\d+)", RegexOptions.IgnoreCase)]
    private static partial Regex SeasonEpisode();

    public static nint VlcHwnd { get; private set; }

    public static string FileName => Path.GetFileName(FilePath) ?? string.Empty;

    public bool IsOpening { get; set { field = value; OnPropertyChanged(); } } = false;

    public bool IsVideoVisible { get; set { field = value; OnPropertyChanged(); } } = false;

    public static string FilePath
    {
        get;
        set
        {
            if (field == value) return;
            field = value;
            FilePathChanged?.Invoke();
        }
    } = string.Empty;

    public string LoadingMessage
    {
        get;
        set
        {
            if (field != value)
            {
                field = value;
                OnPropertyChanged();
            }
        }
    } = "Carregando...";

    public bool IsLoading
    {
        get; set
        {
            if (field == value) return;
            field = value;
            OnPropertyChanged();
        }
    }

    public string OsdMessage
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged();
        }
    } = string.Empty;

    private void OnMediaChanged(object? sender, MediaPlayerMediaChangedEventArgs e)
    {
        // IsLoading = true;
        Utils.AtualizarUI(() =>
        {
            AudioTracks.Clear();
            SubtitleTracks.Clear();
        });
        VlcHwnd = _mediaPlayer.Hwnd;
    }

    private void OnLoadingMessageChanged(object? sender, string e)
    {
        Utils.AtualizarUI(() =>
        {
            LoadingMessage = e;
        });
    }
}
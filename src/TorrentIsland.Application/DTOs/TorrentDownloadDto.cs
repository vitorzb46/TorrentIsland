using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace TorrentIsland.Application.DTOs;

public sealed class TorrentDownloadDto : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    public Guid TorrentId { get; set; }

    public string TorrentName
    {
        get;
        set { if (field != value) { field = value; OnPropertyChanged(); } }
    } = string.Empty;

    public string Status
    {
        get;
        set { if (field != value) { field = value; OnPropertyChanged(); } }
    } = string.Empty;

    public double Progress
    {
        get;
        set { if (field != value) { field = value; OnPropertyChanged(); } }
    }

    public string DownloadSpeed
    {
        get;
        set { if (field != value) { field = value; OnPropertyChanged(); } }
    } = string.Empty;

    public string UploadSpeed
    {
        get;
        set { if (field != value) { field = value; OnPropertyChanged(); } }
    } = string.Empty;

    public int Seeds
    {
        get;
        set { if (field != value) { field = value; OnPropertyChanged(); } }
    }

    public int Peers
    {
        get;
        set { if (field != value) { field = value; OnPropertyChanged(); } }
    }

    public string TimeRemaining
    {
        get;
        set { if (field != value) { field = value; OnPropertyChanged(); } }
    } = string.Empty;
}

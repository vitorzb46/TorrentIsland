namespace TorrentIsland.Application.DTOs;

public sealed class TorrentDownloadDto
{
    public Guid TorrentId { get; set; }
    public string TorrentName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public double Progress { get; set; }
    public string DownloadSpeed { get; set; } = string.Empty;
    public string UploadSpeed { get; set; } = string.Empty;
    public int Seeds { get; set; }
    public int Peers { get; set; }
    public string TimeRemaining { get; set; } = string.Empty;
}

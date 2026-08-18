namespace TorrentIsland.Application.Settings;

public sealed class AppSettings
{
    public string? NomeArquivo { get; set; } = "%(title)s.%(ext)s";
    public string? VideoAudioQualidade { get; set; } = "bestvideo+bestaudio/best";
    public string? UserAgent { get; set; } = string.Empty;
    public string? PastaRecursos { get; set; } = "Resources";
    public string? YtDlpExe { get; set; } = "yt-dlp.exe";
    public string? FfmpegExe { get; set; } = "ffmpeg.exe";
    public string? Cookies { get; set; } = string.Empty;

    // Torrent (MonoTorrent)
    public string? PastaDownloads { get; set; } = "Downloads";
    public string? PastaTorrents { get; set; } = "Torrents";
    public string? PastaCache { get; set; } = "Cache";
    public int TorrentPorta { get; set; } = 51413;
    public int ConnectionsMaxima { get; set; } = 150;
    public int UploadSlotsMaximo { get; set; } = 4;
    // Limites em bytes/s; 0 = ilimitado. Propriedades mantêm o default (0) até serem configuradas.
    public int TorrentLimiteDownload { get; set; }
    public int TorrentLimiteUpload { get; set; }
    public bool TorrentSemear { get; set; } = true;
    public bool TorrentStreaming { get; set; } = true;
    public string[] TorrentTrackers { get; set; } = [];
}

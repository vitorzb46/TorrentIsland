using System.Net;
using System.Net.Sockets;

namespace TorrentIsland.Application.Services;

public sealed class AppSettings
{
    public readonly record struct BootstrapRouter(string Host, int Port);
    static int portaLivre = ObterPortaLivre();
    public string? NomeArquivo { get; } = "%(title)s.%(ext)s";
    public string? VideoAudioQualidade { get; } = "bestvideo+bestaudio/best";
    public string? UserAgent { get; } = string.Empty;
    public string? PastaRecursos { get; } = "Resources";
    public string? YtDlpExe { get; } = "yt-dlp.exe";
    public string? FfmpegExe { get; } = "ffmpeg.exe";
    public string? Cookies { get; } = string.Empty;

    // Torrent (MonoTorrent)
    public string? PastaDownloads { get; } = "Downloads";
    public string? PastaTorrents { get; } = "Torrents";

    // Limites em bytes/s; 0 = ilimitado. Propriedades mantêm o default (0) até serem configuradas.
    public int TorrentLimiteDownload { get; }
    public int TorrentLimiteUpload { get; }

    // Client Engine Settings
    public bool RedirecionarPorta { get; } = true;
    public bool DescobertaPeerLocal { get; } = true;
    public IPEndPoint IpV4 { get; } = new(IPAddress.Any, 0);
    public IPEndPoint IpV6 { get; } = new(IPAddress.Any, 0);
    public int ConnectionsMaxima { get; } = 150;
    public bool LoadFastResume { get; } = true;
    public bool LoadMagnetLinkMetadata { get; } = true;
    public bool LoadDhtCache { get; } = true;
    public string PastaAppData { get; } = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
    public string PastaCache { get; } = "Cache";
    public bool ArquivoParcial { get; } = false;
    public int CacheBytesEmDisco { get; } = 150 * 1024 * 1024;
    public string StreamingPrefix { get; } = $"http://127.0.0.1:{portaLivre}/torrent-stream/";
    public TimeSpan ConexaoTimeout { get; } = TimeSpan.FromSeconds(15);
    public int UploadSlotsMaximo { get; } = 4;
    public List<TimeSpan> Retry { get; } =
    [
        TimeSpan.FromSeconds(15),
        TimeSpan.FromSeconds(30),
        TimeSpan.FromSeconds(60),
        TimeSpan.FromSeconds(120)
    ];
    public List<TimeSpan> PeerTimeout { get; } =
    [
        TimeSpan.FromSeconds(5),
        TimeSpan.FromSeconds(7),
        TimeSpan.FromSeconds(9),
        TimeSpan.FromSeconds(13)
    ];


    private static int ObterPortaLivre()
    {
        using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        socket.Bind(new IPEndPoint(IPAddress.Loopback, 0)); // '0' força o Windows a dar uma porta vazia
        return ((IPEndPoint)socket.LocalEndPoint!).Port;
    }
}

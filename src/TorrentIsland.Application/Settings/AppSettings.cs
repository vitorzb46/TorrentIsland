using System.Net;
using System.Net.Sockets;

namespace TorrentIsland.Application.Settings;

public sealed class AppSettings
{
    // File Logging
    public static bool StackTrace { get; } = true;
    
    // Pastas - AppData
    public static string AppDataFolder { get; } = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
    public static string CacheFolder { get; } = Path.Combine(AppDataFolder, "TorrentIsland", "Cache");
    public static string HtmlsFolder { get; } = Path.Combine(CacheFolder, "htmls");
    public static string TempFolder { get; } = Path.Combine(CacheFolder, "Temp");

    // Pastas - CurrentDirectory
    public static string CurrentFolder { get; } = Path.Combine(AppContext.BaseDirectory);
    public static string DownloadsFolder { get; } = Path.Combine(CurrentFolder, "Downloads");
    public static string TorrentsFolder { get; } = Path.Combine(CurrentFolder, "Torrents");
    public static string ResourcesFolder { get; } = Path.Combine(CurrentFolder, "Resources");

    // Arquivos
    public static string AppLog { get; } = Path.Combine(CurrentFolder, "TorrentIsland.log");
    public static string SubCacheFile { get; } = Path.Combine(CacheFolder, "legendas_cache.json");
    public static string TimeCacheFile { get; } = Path.Combine(CacheFolder, "time_cache.json");
    public static string MkvExtract { get; } = Path.Combine(ResourcesFolder, "mkvextract.exe");
    public static string YtDlpExe { get; } = Path.Combine(ResourcesFolder, "yt-dlp.exe");
    public static string FfmpegExe { get; } = Path.Combine(ResourcesFolder, "ffmpeg.exe");
    public static string ArquivoEngineState { get; } = Path.Combine(CacheFolder, "EngineState");

    // Yt-DLP
    public static bool AllowDownload { get; set; } = false;
    public static string NomeMidia { get; } = "%(title)s.%(ext)s";
    public static string VideoAudioQualidade { get; } = "bestvideo+bestaudio/best";

    // Torrent (MonoTorrent)
    public static string FullPath { get; set; } = string.Empty;
    public static bool OneStream { get; set; } = true;
    public bool Semeando { get; set; } = true;
    public int TorrentLimiteDownload { get; }
    public int TorrentLimiteUpload { get; }

    // Client Engine Settings
    public bool RedirecionarPorta { get; } = true;
    public bool DescobertaPeerLocal { get; } = true;
    public IPEndPoint IpV4 { get; } = new(IPAddress.Any, 0);
    public IPEndPoint IpV6 { get; } = new(IPAddress.Any, 0);
    public int TorrentConnections { get; } = 100;
    public int EngineConnections { get; } = 300;
    public bool LoadFastResume { get; } = true;
    public bool LoadMagnetLinkMetadata { get; } = true;
    public bool LoadDhtCache { get; } = true;
    public bool ArquivoParcial { get; } = false;
    public int CacheBytesEmDisco { get; } = 150 * 1024 * 1024;
    public string StreamingPrefix { get; } = $"http://127.0.0.1:{PortaLivre}/torrent-stream/";
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

    private static int PortaLivre { get; set; } = ObterPortaLivre();

    private static int ObterPortaLivre()
    {
        using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        socket.Bind(new IPEndPoint(IPAddress.Loopback, 0)); // '0' força o Windows a dar uma porta vazia
        return ((IPEndPoint)socket.LocalEndPoint!).Port;
    }
}

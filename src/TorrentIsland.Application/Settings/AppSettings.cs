using System.Net;
using System.Net.Sockets;

namespace TorrentIsland.Application.Settings;

public sealed class AppSettings
{
    // File Logging
    public static bool StackTrace { get; } = false;

    // Pastas - AppData
    public static string AppDataFolder { get; } = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
    public static string CacheFolder { get; } = Path.Combine(AppDataFolder, "TorrentIsland", "Cache");
    public static string HtmlsFolder { get; } = Path.Combine(CacheFolder, "htmls");
    public static string TempFolder { get; } = Path.Combine(CacheFolder, "Temp");
    public static string LimaoFolder { get; } = Path.Combine(HtmlsFolder, "Limao");

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

    // Vlc Configs
    public static bool VlcVerboseLog { get; set; } = true;
    public static bool VlcHttpReconnect { get; set; } = true;
    public static bool VlcDropLateFrames { get; set; } = true;
    public static bool VlcSkipFrames { get; set; } = true;
    public static bool VlcNoVideoTitleShow { get; set; } = true;

    public static int VlcNetworkCachingMs { get; set; } = 5000;
    public static int VlcFileCachingMs { get; set; } = 5000;
    public static int VlcLiveCachingMs { get; set; } = 3000;

    public static bool VlcDirect3DBuffering { get; set; } = true;
    public static bool VlcDirect3D11HwBlending { get; set; } = true;

    public static string VlcHardwareDecoding { get; set; } = "any";
    public static int VlcSkipLoopFilter { get; set; } = 0;

    public static int VlcClockJitterMs { get; set; } = 0;
    public static int VlcSwScaleMode { get; set; } = 2;
    public static int VlcClockSynchro { get; set; } = -1;

    public static bool VlcDeinterlacing { get; set; } = false;
    public static string? VlcDeinterlaceMode { get; set; } = null;

    public static bool VlcSharpenFilter { get; set; } = false;
    public static float? VlcSharpenSigma { get; set; } = 0.05f;

    public static bool VlcSepiaFilter { get; set; } = false;
    public static int VlcSepiaIntensity { get; set; } = 0;

    public static bool VlcGradfunFilter { get; set; } = false;
    public static int? VlcGradfunRadius { get; set; } = 16;
    public static float? VlcGradfunStrength { get; set; } = 1.2f;

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

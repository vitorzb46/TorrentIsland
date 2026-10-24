using System.Net;
using System.Net.Sockets;

namespace TorrentIsland.Application.Settings;

public sealed class AppSettings
{
    // File Logging
    public static bool StackTrace { get; set; } = false;

    // Pastas - AppData
    public static string AppDataFolder { get; set; } = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
    public static string CacheFolder { get; set; } = Path.Combine(AppDataFolder, "TorrentIsland", "Cache");
    public static string HtmlsFolder { get; set; } = Path.Combine(CacheFolder, "htmls");
    public static string TempFolder { get; set; } = Path.Combine(CacheFolder, "Temp");
    public static string LimaoFolder { get; set; } = Path.Combine(HtmlsFolder, "Limao");

    // Pastas - CurrentDirectory
    public static string CurrentFolder { get; set; } = Path.Combine(AppContext.BaseDirectory);
    public static string DownloadsFolder { get; set; } = Path.Combine(CurrentFolder, "Downloads");
    public static string TorrentsFolder { get; set; } = Path.Combine(CurrentFolder, "Torrents");
    public static string ResourcesFolder { get; set; } = Path.Combine(CurrentFolder, "Resources");
    public static string PrintScreenFolder { get; set; } = Path.Combine(CurrentFolder, "Prints");

    // Arquivos
    public static string UserPreferencesJson { get; set; } = Path.Combine(CacheFolder, "UserPreferences.json");
    public static string AppLog { get; set; } = Path.Combine(CurrentFolder, "TorrentIsland.log");
    public static string SubCacheFile { get; set; } = Path.Combine(CacheFolder, "legendas_cache.json");
    public static string TimeCacheFile { get; set; } = Path.Combine(CacheFolder, "time_cache.json");
    public static string MkvExtract { get; set; } = Path.Combine(ResourcesFolder, "mkvextract.exe");
    public static string YtDlpExe { get; set; } = Path.Combine(ResourcesFolder, "yt-dlp.exe");
    public static string FfmpegExe { get; set; } = Path.Combine(ResourcesFolder, "ffmpeg.exe");
    public static string ArquivoEngineState { get; set; } = Path.Combine(CacheFolder, "EngineState");

    // Yt-DLP
    public static bool AllowDownload { get; set; } = false;
    public static string NomeMidia { get; set; } = "%(title)s.%(ext)s";
    public static string VideoAudioQualidade { get; set; } = "bestvideo+bestaudio/best";

    // Vlc Configs
    public static bool VlcVerboseLog { get; set; } = false;
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
    public static int TorrentLimiteDownload { get; set; }
    public static int TorrentLimiteUpload { get; set; }

    // Client Engine Settings
    public static bool RedirecionarPorta { get; set; } = true;
    public static bool DescobertaPeerLocal { get; set; } = true;
    public IPEndPoint IpV4 { get; set; } = new(IPAddress.Any, 0);
    public IPEndPoint IpV6 { get; set; } = new(IPAddress.Any, 0);
    public static int TorrentConnections { get; set; } = 100;
    public static int EngineConnections { get; set; } = 300;
    public static bool LoadFastResume { get; set; } = true;
    public static bool LoadMagnetLinkMetadata { get; set; } = true;
    public static bool LoadDhtCache { get; set; } = true;
    public static bool ArquivoParcial { get; set; } = false;
    public static int CacheBytesEmDisco { get; set; } = 150 * 1024 * 1024;
    public string StreamingPrefix { get; set; } = $"http://127.0.0.1:{PortaLivre}/torrent-stream/";
    public static TimeSpan ConexaoTimeout { get; set; } = TimeSpan.FromSeconds(15);
    public static int UploadSlotsMaximo { get; set; } = 4;
    public static List<TimeSpan> Retry { get; set; } =
    [
        TimeSpan.FromSeconds(15),
        TimeSpan.FromSeconds(30),
        TimeSpan.FromSeconds(60),
        TimeSpan.FromSeconds(120)
    ];
    public static List<TimeSpan> PeerTimeout { get; set; } =
    [
        TimeSpan.FromSeconds(5),
        TimeSpan.FromSeconds(7),
        TimeSpan.FromSeconds(9),
        TimeSpan.FromSeconds(13)
    ];

    public static int PortaLivre { get; set; } = ObterPortaLivre();

    private static int ObterPortaLivre()
    {
        using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        socket.Bind(new IPEndPoint(IPAddress.Loopback, 0)); // '0' força o Windows a dar uma porta vazia
        return ((IPEndPoint)socket.LocalEndPoint!).Port;
    }
}

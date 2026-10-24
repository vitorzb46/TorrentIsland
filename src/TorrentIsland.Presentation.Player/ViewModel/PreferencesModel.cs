using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using TorrentIsland.Application.Settings;
using TorrentIsland.Infrastructure.Services;

namespace TorrentIsland.Presentation.Player.ViewModel;

public class PreferencesModel : INotifyPropertyChanged
{
    // VLC
    public bool VlcVerboseLog { get => field; set { field = value; OnPropertyChanged(); } }
    public bool VlcHttpReconnect { get => field; set { field = value; OnPropertyChanged(); } }
    public bool VlcDropLateFrames { get => field; set { field = value; OnPropertyChanged(); } }
    public bool VlcSkipFrames { get => field; set { field = value; OnPropertyChanged(); } }
    public bool VlcNoVideoTitleShow { get => field; set { field = value; OnPropertyChanged(); } }

    public int VlcNetworkCachingMs { get => field; set { field = value; OnPropertyChanged(); } }
    public int VlcFileCachingMs { get => field; set { field = value; OnPropertyChanged(); } }
    public int VlcLiveCachingMs { get => field; set { field = value; OnPropertyChanged(); } }

    public bool VlcDirect3DBuffering { get => field; set { field = value; OnPropertyChanged(); } }
    public bool VlcDirect3D11HwBlending { get => field; set { field = value; OnPropertyChanged(); } }

    public string VlcHardwareDecoding { get => field; set { field = value; OnPropertyChanged(); } } = string.Empty;
    public int VlcSkipLoopFilter { get => field; set { field = value; OnPropertyChanged(); } }

    public int VlcClockJitterMs { get => field; set { field = value; OnPropertyChanged(); } }
    public int VlcSwScaleMode { get => field; set { field = value; OnPropertyChanged(); } }
    public int VlcClockSynchro { get => field; set { field = value; OnPropertyChanged(); } }

    public bool VlcDeinterlacing { get => field; set { field = value; OnPropertyChanged(); } }
    public string? VlcDeinterlaceMode { get => field; set { field = value; OnPropertyChanged(); } }

    public bool VlcSharpenFilter { get => field; set { field = value; OnPropertyChanged(); } }
    public float? VlcSharpenSigma { get => field; set { field = value; OnPropertyChanged(); } }

    public bool VlcSepiaFilter { get => field; set { field = value; OnPropertyChanged(); } }
    public int VlcSepiaIntensity { get => field; set { field = value; OnPropertyChanged(); } }

    public bool VlcGradfunFilter { get => field; set { field = value; OnPropertyChanged(); } }
    public int? VlcGradfunRadius { get => field; set { field = value; OnPropertyChanged(); } }
    public float? VlcGradfunStrength { get => field; set { field = value; OnPropertyChanged(); } }


    // TORRENT
    public int TorrentLimiteDownload { get => field; set { field = value; OnPropertyChanged(); } }
    public int TorrentLimiteUpload { get => field; set { field = value; OnPropertyChanged(); } }
    public int TorrentConnections { get => field; set { field = value; OnPropertyChanged(); } }


    // TORRENT (GLOBAL)
    public bool RedirecionarPorta { get => field; set { field = value; OnPropertyChanged(); } }
    public bool DescobertaPeerLocal { get => field; set { field = value; OnPropertyChanged(); } }
    public bool LoadFastResume { get => field; set { field = value; OnPropertyChanged(); } }
    public bool LoadMagnetLinkMetadata { get => field; set { field = value; OnPropertyChanged(); } }
    public bool LoadDhtCache { get => field; set { field = value; OnPropertyChanged(); } }
    public int UploadSlotsMaximo { get => field; set { field = value; OnPropertyChanged(); } }
    public int PortaLivre { get => field; set { field = value; OnPropertyChanged(); } }
    public List<TimeSpan> Retry { get => field; set { field = value; OnPropertyChanged(); } } = [];
    public List<TimeSpan> PeerTimeout { get => field; set { field = value; OnPropertyChanged(); } } = [];


    // YT-DLP
    public bool AllowDownload { get => field; set { field = value; OnPropertyChanged(); } }
    
    /// <summary>
    /// Tenta carregar do arquivo JSON. Se não existir ou falhar, carrega do AppSettings padrão.
    /// </summary>
    public void LoadFromJsonOrDefaults()
    {
        var jsonFile = AppSettings.UserPreferencesJson;
        if (File.Exists(jsonFile))
        {
            try
            {
                var savedModel = JsonFiles.LoadFromFile<PreferencesModel>(jsonFile);

                if (savedModel != null)
                {
                    VlcVerboseLog = savedModel.VlcVerboseLog;
                    VlcHttpReconnect = savedModel.VlcHttpReconnect;
                    VlcDropLateFrames = savedModel.VlcDropLateFrames;
                    VlcSkipFrames = savedModel.VlcSkipFrames;
                    VlcNoVideoTitleShow = savedModel.VlcNoVideoTitleShow;
                    VlcNetworkCachingMs = savedModel.VlcNetworkCachingMs;
                    VlcFileCachingMs = savedModel.VlcFileCachingMs;
                    VlcLiveCachingMs = savedModel.VlcLiveCachingMs;
                    VlcDirect3DBuffering = savedModel.VlcDirect3DBuffering;
                    VlcDirect3D11HwBlending = savedModel.VlcDirect3D11HwBlending;
                    VlcHardwareDecoding = savedModel.VlcHardwareDecoding;
                    VlcSkipLoopFilter = savedModel.VlcSkipLoopFilter;
                    VlcClockJitterMs = savedModel.VlcClockJitterMs;
                    VlcSwScaleMode = savedModel.VlcSwScaleMode;
                    VlcClockSynchro = savedModel.VlcClockSynchro;
                    VlcDeinterlacing = savedModel.VlcDeinterlacing;
                    VlcDeinterlaceMode = savedModel.VlcDeinterlaceMode;
                    VlcSharpenFilter = savedModel.VlcSharpenFilter;
                    VlcSharpenSigma = savedModel.VlcSharpenSigma;
                    VlcSepiaFilter = savedModel.VlcSepiaFilter;
                    VlcSepiaIntensity = savedModel.VlcSepiaIntensity;
                    VlcGradfunFilter = savedModel.VlcGradfunFilter;
                    VlcGradfunRadius = savedModel.VlcGradfunRadius;
                    VlcGradfunStrength = savedModel.VlcGradfunStrength;

                    TorrentLimiteDownload = savedModel.TorrentLimiteDownload;
                    TorrentLimiteUpload = savedModel.TorrentLimiteUpload;
                    TorrentConnections = savedModel.TorrentConnections;

                    RedirecionarPorta = savedModel.RedirecionarPorta;
                    DescobertaPeerLocal = savedModel.DescobertaPeerLocal;
                    LoadFastResume = savedModel.LoadFastResume;
                    LoadMagnetLinkMetadata = savedModel.LoadMagnetLinkMetadata;
                    LoadDhtCache = savedModel.LoadDhtCache;
                    UploadSlotsMaximo = savedModel.UploadSlotsMaximo;
                    Retry = savedModel.Retry;
                    PeerTimeout = savedModel.PeerTimeout;
                    PortaLivre = savedModel.PortaLivre;

                    AllowDownload = savedModel.AllowDownload;

                    return;
                }
            }
            catch (Exception) { /* best effort */ }
        }

        LoadFromAppSettings();
    }

    /// <summary>
    /// Carrega direto do <see cref="AppSettings"/>
    /// </summary>
    public void LoadFromAppSettings()
    {
        VlcVerboseLog = AppSettings.VlcVerboseLog;
        VlcHttpReconnect = AppSettings.VlcHttpReconnect;
        VlcDropLateFrames = AppSettings.VlcDropLateFrames;
        VlcSkipFrames = AppSettings.VlcSkipFrames;
        VlcNoVideoTitleShow = AppSettings.VlcNoVideoTitleShow;
        VlcNetworkCachingMs = AppSettings.VlcNetworkCachingMs;
        VlcFileCachingMs = AppSettings.VlcFileCachingMs;
        VlcLiveCachingMs = AppSettings.VlcLiveCachingMs;
        VlcDirect3DBuffering = AppSettings.VlcDirect3DBuffering;
        VlcDirect3D11HwBlending = AppSettings.VlcDirect3D11HwBlending;
        VlcHardwareDecoding = AppSettings.VlcHardwareDecoding;
        VlcSkipLoopFilter = AppSettings.VlcSkipLoopFilter;
        VlcClockJitterMs = AppSettings.VlcClockJitterMs;
        VlcSwScaleMode = AppSettings.VlcSwScaleMode;
        VlcClockSynchro = AppSettings.VlcClockSynchro;
        VlcDeinterlacing = AppSettings.VlcDeinterlacing;
        VlcDeinterlaceMode = AppSettings.VlcDeinterlaceMode;
        VlcSharpenFilter = AppSettings.VlcSharpenFilter;
        VlcSharpenSigma = AppSettings.VlcSharpenSigma;
        VlcSepiaFilter = AppSettings.VlcSepiaFilter;
        VlcSepiaIntensity = AppSettings.VlcSepiaIntensity;
        VlcGradfunFilter = AppSettings.VlcGradfunFilter;
        VlcGradfunRadius = AppSettings.VlcGradfunRadius;
        VlcGradfunStrength = AppSettings.VlcGradfunStrength;

        TorrentLimiteDownload = AppSettings.TorrentLimiteDownload;
        TorrentLimiteUpload = AppSettings.TorrentLimiteUpload;
        TorrentConnections = AppSettings.TorrentConnections;

        RedirecionarPorta = AppSettings.RedirecionarPorta;
        DescobertaPeerLocal = AppSettings.DescobertaPeerLocal;
        LoadFastResume = AppSettings.LoadFastResume;
        LoadMagnetLinkMetadata = AppSettings.LoadMagnetLinkMetadata;
        LoadDhtCache = AppSettings.LoadDhtCache;
        UploadSlotsMaximo = AppSettings.UploadSlotsMaximo;
        Retry = AppSettings.Retry;
        PeerTimeout = AppSettings.PeerTimeout;
        PortaLivre = AppSettings.PortaLivre;

        AllowDownload = AppSettings.AllowDownload;
    }


    /// <summary>
    /// Pega os valores alterados na tela e salva de volta no AppSettings (estático).
    /// </summary>
    public void SaveInAppSettings()
    {
        // Vlc
        AppSettings.VlcVerboseLog = VlcVerboseLog;
        AppSettings.VlcHttpReconnect = VlcHttpReconnect;
        AppSettings.VlcDropLateFrames = VlcDropLateFrames;
        AppSettings.VlcSkipFrames = VlcSkipFrames;
        AppSettings.VlcNoVideoTitleShow = VlcNoVideoTitleShow;
        AppSettings.VlcNetworkCachingMs = VlcNetworkCachingMs;
        AppSettings.VlcFileCachingMs = VlcFileCachingMs;
        AppSettings.VlcLiveCachingMs = VlcLiveCachingMs;
        AppSettings.VlcDirect3DBuffering = VlcDirect3DBuffering;
        AppSettings.VlcDirect3D11HwBlending = VlcDirect3D11HwBlending;
        AppSettings.VlcHardwareDecoding = VlcHardwareDecoding;
        AppSettings.VlcSkipLoopFilter = VlcSkipLoopFilter;
        AppSettings.VlcClockJitterMs = VlcClockJitterMs;
        AppSettings.VlcSwScaleMode = VlcSwScaleMode;
        AppSettings.VlcClockSynchro = VlcClockSynchro;
        AppSettings.VlcDeinterlacing = VlcDeinterlacing;
        AppSettings.VlcDeinterlaceMode = VlcDeinterlaceMode;
        AppSettings.VlcSharpenFilter = VlcSharpenFilter;
        AppSettings.VlcSharpenSigma = VlcSharpenSigma;
        AppSettings.VlcSepiaFilter = VlcSepiaFilter;
        AppSettings.VlcSepiaIntensity = VlcSepiaIntensity;
        AppSettings.VlcGradfunFilter = VlcGradfunFilter;
        AppSettings.VlcGradfunRadius = VlcGradfunRadius;
        AppSettings.VlcGradfunStrength = VlcGradfunStrength;

        // Torrent
        AppSettings.TorrentLimiteDownload = TorrentLimiteDownload;
        AppSettings.TorrentLimiteUpload = TorrentLimiteUpload;
        AppSettings.TorrentConnections = TorrentConnections;

        // Torrent (Global)
        AppSettings.RedirecionarPorta = RedirecionarPorta;
        AppSettings.DescobertaPeerLocal = DescobertaPeerLocal;
        AppSettings.LoadFastResume = LoadFastResume;
        AppSettings.LoadMagnetLinkMetadata = LoadMagnetLinkMetadata;
        AppSettings.LoadDhtCache = LoadDhtCache;
        AppSettings.UploadSlotsMaximo = UploadSlotsMaximo;
        AppSettings.Retry = Retry;
        AppSettings.PeerTimeout = PeerTimeout;

        // Yt-DLP
        AppSettings.AllowDownload = AllowDownload;
    }
    
    public event PropertyChangedEventHandler? PropertyChanged;
    
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
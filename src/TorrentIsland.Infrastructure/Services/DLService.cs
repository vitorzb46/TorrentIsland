using TorrentIsland.Application.Interfaces;
using TorrentIsland.Application.Settings;
using TorrentIsland.Infrastructure.Logging;
using YoutubeDLSharp;

namespace TorrentIsland.Infrastructure.Services;

public class DLService : IDLService
{
    private readonly YoutubeDL _youtubeDL;
    private readonly string _toolsPath = Path.Combine(AppContext.BaseDirectory, "tools");
    private readonly string _ytDlpPath = Path.Combine(AppContext.BaseDirectory, "tools", "yt-dlp.exe");
    private readonly string _ffmpegPath = Path.Combine(AppContext.BaseDirectory, "tools", "ffmpeg.exe");
    public DLService()
    {
        Directory.CreateDirectory(_toolsPath);

        _ = CheckBinaries();

        _youtubeDL = new YoutubeDL
        {
            YoutubeDLPath = _ytDlpPath,
            FFmpegPath = _ffmpegPath,
            OutputFolder = AppSettings.PastaDownloads,
            OutputFileTemplate = "%(title)s.%(ext)s",
        };
    }

    private async Task CheckBinaries()
    {
        await Utils.DownloadBinaries(directoryPath: _toolsPath);
    }

    public async Task<string> GetStreamingUrl(string url)
    {
        var fetchData = await _youtubeDL.RunVideoDataFetch(url);

        if (!fetchData.Success || fetchData.Data == null)
        {
            Log.Salvar("Erro ao obter dados do stream: " + fetchData.ErrorOutput);
            return string.Empty;
        }

        // var directUrl = fetchData.Data.Url;
        var directUrl = fetchData.Data.Formats.FirstOrDefault(x => x.FormatNote == "720p")?.Url;

        if (string.IsNullOrEmpty(directUrl))
        {
            Log.Salvar("Erro ao obter url do stream: ");
            return string.Empty;
        }

        return directUrl;
    }
}
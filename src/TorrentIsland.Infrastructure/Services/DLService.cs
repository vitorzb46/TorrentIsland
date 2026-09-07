using TorrentIsland.Application.Interfaces;
using TorrentIsland.Application.Settings;
using TorrentIsland.Infrastructure.Logging;
using YoutubeDLSharp;

namespace TorrentIsland.Infrastructure.Services;

public class DLService : IDLService
{
    private readonly YoutubeDL _youtubeDL;
    private readonly string ResourcesFolder = AppSettings.ResourcesFolder;
    public DLService()
    {
        Directory.CreateDirectory(ResourcesFolder);
        
        _youtubeDL = new YoutubeDL
        {
            YoutubeDLPath = AppSettings.YtDlpExe,
            FFmpegPath = AppSettings.FfmpegExe,
            OutputFolder = AppSettings.DownloadsFolder,
            OutputFileTemplate = AppSettings.NomeMidia,
        };
    }

    public async Task CheckBinariesAsync()
    {
        await Utils.DownloadBinaries(directoryPath: ResourcesFolder);
    }

    public async Task<string> GetStreamingUrl(string url)
    {
        var fetchData = await _youtubeDL.RunVideoDataFetch(url);

        if (!fetchData.Success || fetchData.Data == null)
        {
            Log.Salvar("Erro ao obter dados do stream: " + fetchData.ErrorOutput);
            return string.Empty;
        }

        var directUrl = fetchData.Data.Formats.FirstOrDefault(x => x.FormatNote == "720p")?.Url;

        if (string.IsNullOrEmpty(directUrl))
        {
            Log.Salvar("Erro ao obter url do stream: ");
            return string.Empty;
        }

        return directUrl;
    }
}
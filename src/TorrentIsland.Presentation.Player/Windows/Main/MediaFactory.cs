using System.IO;
using LibVLCSharp.Shared;
using TorrentIsland.Application.Interfaces;
using TorrentIsland.Infrastructure.Interfaces;
using TorrentIsland.Infrastructure.Services;

namespace TorrentIsland.Presentation.Player.Windows.Main;

public sealed class MediaFactory(LibVLC libVLC, IDLService dL, IManagers managers)
{
    public sealed record ResolvedMedia(Media? Media, string FilePath);
    private readonly LibVLC _libVlc = libVLC;
    private readonly IDLService _ytDl = dL;
    private readonly IManagers _managers = managers;
    private Media? _media;
    private bool _isTorrent;
    private string? _filePath;

    public async Task<ResolvedMedia?> CreateAsync(string filePathOrUrl)
    {
        ArgumentException.ThrowIfNullOrEmpty(filePathOrUrl);

        NewIteration(filePathOrUrl);
        CheckIfFileExists();
        await CheckIfComesFromUrl();

        ArgumentException.ThrowIfNullOrEmpty(_filePath);
        GenerateMkvHash();        
        return new ResolvedMedia(_media, _filePath);
    }

    private void NewIteration(string filePathOrUrl)
    {
        _media = null;
        _isTorrent = false;
        _filePath = filePathOrUrl;
    }

    private void CheckIfFileExists()
    {
        if (!File.Exists(_filePath)) return;
        _media = new Media(_libVlc, _filePath, FromType.FromPath);
    }

    private async Task CheckIfComesFromUrl()
    {
        if (_media != null) return;
        if (!Uri.TryCreate(_filePath, UriKind.Absolute, out _)) return;

        string url = await ResolveUrl();
        if (string.IsNullOrEmpty(url)) return;

        await IsTorrentStream();

        _media = new Media(_libVlc, url, FromType.FromLocation);
    }
    
    private async Task<string> ResolveUrl()
    {
        bool magnet = _filePath!.StartsWith("magnet");
        bool stream = _filePath!.StartsWith("http://127.0.0.1");
        if (magnet || stream)
        {
            _isTorrent = true;
            return _filePath;
        }
        return await _ytDl.GetStreamingUrl(_filePath);
    }

    private async Task IsTorrentStream()
    {
        if (!_isTorrent) return;
        var torrentMap = await _managers.ObterTorrentsAsync();
        if (torrentMap.Count == 0) return;

        _filePath = torrentMap.Select(v => v.Value.FullPath).FirstOrDefault();
    }

    private void GenerateMkvHash()
    {
        var ext = Path.GetExtension(_filePath);
        if (ext != ".mkv") return;
        KeyGenerator.Hash(_filePath);
    }
}
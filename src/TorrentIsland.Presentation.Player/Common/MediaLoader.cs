using System.IO;
using TorrentIsland.Presentation.Player.Windows.Main;
using MessageBox = System.Windows.MessageBox;

namespace TorrentIsland.Presentation.Player.Common;

public sealed class MediaLoader(PlayerWindow playerWindow)
{
    private readonly PlayerWindow _playerWindow = playerWindow;

    public async Task<bool> InputAsync(string? text) => await InputAsync(null, text);
    public async Task<bool> InputAsync(string[]? filePath, string? text)
    {
        static string filepath(string[] fp) => string.IsNullOrEmpty(fp[0]) ?
            string.Empty : Path.GetExtension(fp[0]).ToLowerInvariant();
            
        var x = (filePath, text) switch
        {
            (string[] f, _) when f.Length > 0 && Utils.ExtensoesVideo.Contains(filepath(f)) =>
                    await Media(f[0]),

            (string[] t, _) when t.Length > 0 && Utils.ExtensaoTorrent.Contains(filepath(t)) =>
                    await Torrent(t[0]),

            (_, string m) when m != null &&
                (m.StartsWith("magnet:?", StringComparison.OrdinalIgnoreCase) ||
                m.StartsWith("http://itorrents.net", StringComparison.OrdinalIgnoreCase)) =>
                    await Torrent(m),

            (_, string yt) when !string.IsNullOrWhiteSpace(yt) =>
                    await Media(yt),

            (_, _) => InvalidUrl()
        };
        return x;
    }

    private async Task<bool> Media(string filePath)
    {
        try
        {
            await _playerWindow.CarregarMidiaAsync(filePath);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
        
    }

    private async Task<bool> Torrent(string filePath)
    {
        try
        {
            await _playerWindow.CarregarStreamTorrentAsync(filePath);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private static bool InvalidUrl()
    {
        MessageBox.Show("URL inválida!", "ERRO", System.Windows.MessageBoxButton.OK);
        return false;
    }
}
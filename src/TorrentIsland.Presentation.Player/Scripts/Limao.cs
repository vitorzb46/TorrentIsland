using System.Collections.ObjectModel;
using System.IO;
using System.Net.Http;
using HtmlAgilityPack;

namespace TorrentIsland.Presentation.Player.Scripts;

public class Limao
{
    private static readonly string CacheFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "TorrentIsland",
        "Cache",
        "htmls",
        "Limao");

    private static string HtmlPath { get; set; } = string.Empty;
    private static readonly DateTime LimitDate = DateTime.Now.AddDays(-7);
    private static readonly HttpClient _client;
    private static string url = string.Empty;
    private static string htmlFile = string.Empty;
    static Limao()
    {
        _client = new HttpClient();
        CanDelete();
        Directory.CreateDirectory(CacheFolder);
    }

    public static async Task<string> GetUrlMagneticAsync(string torrentName)
    {
        var magnet = string.Empty;
        var doc = await LoadFromCacheOrWebAsync();

        if (doc == null) return magnet;

        var torrent = doc?.DocumentNode.SelectSingleNode($".//a[contains(text(),'{torrentName}')]");

        if (torrent == null) return magnet;

        url = string.Concat("https://www.limetorrents.fun", torrent.GetAttributeValue("href", "N/A"));

        htmlFile = string.Concat(torrentName, ".html");
        HtmlPath = Path.Combine(CacheFolder, htmlFile);
        
        var doc2 = await LoadFromCacheOrWebAsync();

        magnet = doc2.DocumentNode.SelectSingleNode(".//a[contains(text(),'Magnet Download')]")
                                  .GetAttributeValue("href", "N/A");

        return magnet;
    }

    public static async Task<ObservableCollection<TorrentSearchDto>> SearchAsync(string query)
    {
        var results = new ObservableCollection<TorrentSearchDto>();
        url = $"https://www.limetorrents.fun/search/all/{query}/seeds/1/";
        htmlFile = string.Concat(query, ".html");
        HtmlPath = Path.Combine(CacheFolder, htmlFile);

        try
        {
            var doc = await LoadFromCacheOrWebAsync();

            if (doc == null) return results;

            var rows = doc?.DocumentNode.SelectNodes("//table[contains(@class,'table2')]//tr[td]");

            if (rows is null) return results;

            foreach (var row in rows)
            {
                var ttNameDiv = row.SelectSingleNode(".//div[contains(@class,'tt-name')]");
                if (ttNameDiv == null) continue;

                var downloadLinkNode = ttNameDiv.SelectSingleNode(".//a[contains(@href,'itorrents.net')]");
                string downloadUrl = downloadLinkNode!.GetAttributeValue("href", "N/A");

                var nameLinkNode = ttNameDiv.SelectSingleNode(".//a[not(contains(@href,'itorrents.net'))]");
                string torrentName = nameLinkNode?.InnerText.Trim() ?? "N/A";

                var tds = row.SelectNodes(".//td");
                if (tds == null || tds.Count < 6) continue;

                string size = tds[2].InnerText.Trim();
                string seed = tds[3].InnerText.Trim();
                string leech = tds[4].InnerText.Trim();

                results.Add(new TorrentSearchDto
            (downloadUrl, torrentName, size, seed, leech));
            }

            return results;
        }
        catch (Exception ex)
        {
            Log.Salvar($"Erro no script Limao: {ex.Message}");
            return results;
        }
    }

    private static async Task<HtmlDocument> LoadFromCacheOrWebAsync()
    {
        string loadHtml = File.Exists(HtmlPath)
                        ? loadHtml = File.ReadAllText(HtmlPath)
                        : loadHtml = await _client.GetStringAsync(url);

        HtmlDocument doc = new();
        doc.LoadHtml(loadHtml);
        SaveHtml(loadHtml);
        return doc!;
    }

    private static void SaveHtml(string loadHtml)
    {
        if (!File.Exists(HtmlPath))
        {
            File.WriteAllText(HtmlPath, loadHtml);
        }
    }

    private static void CanDelete()
    {
        if (File.Exists(HtmlPath))
        {
            var last = File.GetLastWriteTime(HtmlPath);
            if (last >= LimitDate)
            {
                File.Delete(HtmlPath);
            }
        }
    }
}

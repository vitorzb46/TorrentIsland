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
    static Limao()
    {
        _client = new HttpClient();
        CanDelete();
        Directory.CreateDirectory(CacheFolder);
    }

    public static async Task<ObservableCollection<TorrentSearchDto
>> SearchAsync(string query)
    {
        var results = new ObservableCollection<TorrentSearchDto
    >();
        string url = $"https://www.limetorrents.fun/search/all/{query}/seeds/1/";
        string htmlFile = string.Concat(query, ".html");
        string loadHtml = string.Empty;
        HtmlPath = Path.Combine(CacheFolder, htmlFile);
        
        try
        {
            if (File.Exists(HtmlPath))
            {
                Log.Salvar($"Carregando HTML do cache: {HtmlPath}");
                loadHtml = File.ReadAllText(Path.Combine(CacheFolder, HtmlPath));
            }
            else
            {
                Log.Salvar($"Carregando HTML da web: {url}");
                loadHtml = await _client.GetStringAsync(url);
            }

            HtmlDocument doc = new();
            doc.LoadHtml(loadHtml);

            if (doc != null && !File.Exists(HtmlPath))
            {
                File.WriteAllText(HtmlPath, loadHtml);
            }

            var rows = doc.DocumentNode.SelectNodes("//table[contains(@class,'table2')]//tr[td]");

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

using System.Collections.ObjectModel;
using System.IO;
using System.Net.Http;
using HtmlAgilityPack;

namespace TorrentIsland.Presentation.Player.Scripts;

public class Limao
{
    //LinkDownload - TorrentName - Size - Seed - Leech
    public record TorrentSearch(string LinkDownload, string TorrentName, string Size, string Seed, string Leech);
    private static readonly HttpClient _client;
    static Limao()
    {
        _client = new HttpClient();
    }

    public static async Task<ObservableCollection<TorrentSearch>> SearchAsync(string query)
    {
        var results = new ObservableCollection<TorrentSearch>();
        string url = $"https://www.limetorrents.fun/search/all/{query}/seeds/1/";

        try
        {
            // string html = await _client.GetStringAsync(url);
            string html = File.ReadAllText("asd.html");

            HtmlDocument doc = new();
            doc.LoadHtml(html);

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

                results.Add(new TorrentSearch(downloadUrl, torrentName, size, seed, leech));
            }

            return results;
        }
        catch (Exception ex)
        {
            Log.Salvar($"Erro no script Limao: {ex.Message}");
            return results;
        }
    }
}

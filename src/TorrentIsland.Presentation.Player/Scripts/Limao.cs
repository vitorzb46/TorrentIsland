using HtmlAgilityPack;
using System.Collections.ObjectModel;
using System.Net.Http;

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
            string html = await _client.GetStringAsync(url);

            HtmlDocument doc = new();
            doc.LoadHtml(html);

            var rows = doc.DocumentNode.SelectNodes("//table[@class='table2']/tbody");

            if (rows is null) return null!;

            foreach (var row in rows)
            {
                var link = row.SelectNodes("//td/div[@class='tt-name']/a[1]"); //GetAttributeValue href
                var name = row.SelectNodes("//td/div[@class='tt-name']/a[2]"); //innerText
                var size = row.SelectNodes("//tr/td[@class='tdnormal'][2]"); //innerText
                var seed = row.SelectNodes("//tr/td[@class='tdseed']"); //innerText
                var leech = row.SelectNodes("//tr/td[@class='tdleech']"); //innerText

                if (link is not null && name is not null && size is not null && seed is not null && leech is not null)
                {

                }
            }
        }
        catch (Exception ex)
        {
            Log.Salvar($"Erro no script Limao: {ex.Message}");
        }
    }
}

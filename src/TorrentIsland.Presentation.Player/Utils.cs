using System.IO;

namespace TorrentIsland.Presentation.Player;

public class Utils
{
    public static readonly string[] ExtensoesVideo = [
        ".mp4", ".mkv", ".avi", ".mov", ".wmv", ".flv", ".webm",
        ".m4v", ".ts", ".m2ts", ".vob", ".mpg", ".mpeg", ".3gp", ".ogv"
    ];
    public static readonly string[] ExtensaoTorrent = [".torrent"];
    public static readonly string[] ExtensoesSubs = [
        ".srt", ".vtt", ".ssa", ".ass",
    ];
    /// <summary>
    /// Executa ação síncrona e atualiza a thread principal da UI.
    /// </summary>
    /// <param name="callback">Ação para executar de forma síncrona.</param>
    /// <returns>Uma task que representa operação síncrona.</returns>
    public static void AtualizarUI(Action callback)
    {
        System.Windows.Application.Current.Dispatcher.Invoke(callback);
    }
    /// <summary>
    /// Executa ação assíncrona e atualiza a thread principal da UI.
    /// </summary>
    /// <param name="callback">Ação para executar de forma assíncrona.</param>
    /// <returns>Uma task que representa operação assíncrona.</returns>
    public static async Task AtualizarUIAsync(Func<Task> callbackAsync)
    {
        await System.Windows.Application.Current.Dispatcher.InvokeAsync(callbackAsync);
    }
    /// <summary>
    /// Retorna o tamanho do arquivo em MB ou GB.
    /// </summary>
    /// <param name="filePath">Caminho do arquivo</param>
    /// <returns></returns>
    public static string? BytesFormat(string filePath)
    {
        var infoArquivo = new FileInfo(filePath);
        long tamanhoBytes = infoArquivo.Length;

        if (tamanhoBytes >= 1024 * 1024 * 1024) // 1 GB
        {
            return $"{tamanhoBytes / (1024.0 * 1024.0 * 1024.0):F2} GB";
        }
        else
        {
            return $"{tamanhoBytes / (1024.0 * 1024.0):F2} MB";
        }
    }
    /// <summary>
    /// Verifica o código de idioma e retorna o nome da língua correspondente.
    /// </summary>
    /// <param name="valor">O código de idioma a ser comparado.</param>
    /// <returns>O nome traduzido, ou null se o código for inválido.</returns>
    public static string? TraduzirIdioma(string valor)
    {
        if (!EhIdiomaValido(valor)) return null;

        var chave = valor.Trim().ToLowerInvariant();

        return Idiomas.TryGetValue(chave, out var idioma) ? idioma : null;
    }
    /// <summary>Indica se o código de idioma é realmente um idioma (não "und"/"undetermined"/vazio).</summary>
    public static bool EhIdiomaValido(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor)) return false;
        var limpo = valor.Trim().ToLower();
        return limpo is not ("und" or "undetermined" or "unknown" or "mis" or "mul" or "zxx" or "???");
    }
    /// <summary>Indica se o valor representa "idioma indefinido" (ex.: "und").</summary>
    public static bool EhIdiomaIndefinido(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor)) return true;
        return !EhIdiomaValido(valor);
    }

    private static readonly Dictionary<string, string> Idiomas = new(StringComparer.OrdinalIgnoreCase)
    {
        ["pt"] = "Português",
        ["en"] = "Inglês",
        ["es"] = "Espanhol",
        ["fr"] = "Francês",
        ["de"] = "Alemão",
        ["it"] = "Italiano",
        ["ja"] = "Japonês",
        ["ko"] = "Coreano",
        ["zh"] = "Chinês",
        ["ru"] = "Russo",
        ["ar"] = "Árabe",
        ["hi"] = "Hindi",
        ["nl"] = "Holandês",
        ["sv"] = "Sueco",
        ["pl"] = "Polonês",
        ["zh-hant"] = "Chinês (Tradicional)",
        ["zh-hans"] = "Chinês (Simplificado)",
        ["da"] = "Dinamarquês",
        ["et"] = "Estoniano",
        ["fi"] = "Finlandês",
        ["cs"] = "Tcheco",
        ["bg"] = "Búlgaro",
        ["el"] = "Grego",
        ["iw"] = "Hebraico",
        ["he"] = "Hebraico",
        ["hu"] = "Húngaro",
        ["lv"] = "Letão",
        ["ms"] = "Malaio",
        ["ro"] = "Romeno",
        ["lt"] = "Lituano",
        ["no"] = "Norueguês",
        ["ta"] = "Tâmil",
        ["sk"] = "Eslovaco",
        ["th"] = "Tailandês",
        ["te"] = "Telugu",
        ["sl"] = "Esloveno",
        ["tr"] = "Turco",
        ["vi"] = "Vietnamita",
        ["uk"] = "Ucraniano",
        ["id"] = "Indonésio",
        ["sr"] = "Sérvia",
    };
}
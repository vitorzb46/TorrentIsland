using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace TorrentIsland.Presentation.Player;

public class Utils
{
    private const int GCLP_HBRBACKGROUND = -10;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr FindWindowEx(IntPtr parentHandle, IntPtr childAfter, string? className, string? windowTitle);
    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetClassLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    [DllImport("gdi32.dll", SetLastError = true)]
    private static extern IntPtr CreateSolidBrush(uint crColor);

    [DllImport("gdi32.dll", SetLastError = true)]
    private static extern bool DeleteObject(IntPtr hObject);

    [DllImport("user32.dll")]
    private static extern bool InvalidateRect(IntPtr hWnd, IntPtr lpRect, bool bErase);

    public static readonly string[] ExtensoesVideo = [
        ".mp4", ".mkv", ".avi", ".mov", ".wmv", ".flv", ".webm",
        ".m4v", ".ts", ".m2ts", ".vob", ".mpg", ".mpeg", ".3gp", ".ogv"
    ];
    public static readonly string[] ExtensaoTorrent = [".torrent"];
    public static readonly string[] ExtensoesSubs = [
        ".srt", ".vtt", ".ssa", ".ass",
    ];


    /// <summary>
    /// Altera a cor de fundo da janela do VideoView para preto antes de iniciar a mídia.
    /// </summary>
    /// <remarks>
    /// Este método é chamado ao iniciar uma mídia,
    /// para prevenir a exibição do fundo branco do VideoView.
    /// </remarks>
    public static void VideoView_Background_Black()
    {
        IntPtr mainHwnd = PlayerViewModel.VlcHwnd;

        IntPtr vlcChildHwnd = FindWindowEx(mainHwnd, IntPtr.Zero, null, null);
        if (vlcChildHwnd == IntPtr.Zero)
            vlcChildHwnd = mainHwnd;

        IntPtr hBrush = CreateSolidBrush(0x00000000);
        IntPtr oldBrush = SetClassLongPtr(vlcChildHwnd, GCLP_HBRBACKGROUND, hBrush);
        if (oldBrush != IntPtr.Zero) DeleteObject(oldBrush);

        InvalidateRect(vlcChildHwnd, IntPtr.Zero, true);
    }


    /// <summary>
    /// Tenta obter dados do tipo Data Object usando o formato especificado.
    /// </summary>
    /// <typeparam name="T">O tipo de dado a ser obtido.</typeparam>
    /// <param name="dataObject">O IDataObject de onde obtem os dados.</param>
    /// <param name="formato">O formato dos dados.</param>
    /// <param name="resultado">Os dados obtidos.</param>
    /// <returns>True se os dados foram obtidos com sucesso e convertidos, false caso contrário.</returns>
    public static bool TryGetDataObject<T>(IDataObject dataObject, string formato, out T resultado)
    {
        resultado = default!;

        if (!dataObject.GetDataPresent(formato))
            return false;

        var dado = dataObject.GetData(formato);
        if (dado is T dadoConvertido)
        {
            if (dadoConvertido is string[] array && array.Length == 0)
                return false;

            resultado = dadoConvertido;
            return true;
        }

        return false;
    }


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
        try
        {
            if (!File.Exists(filePath))
                return "Arquivo não encontrado";

            var tamanhoBytes = new FileInfo(filePath).Length;

            string[] unidades = ["B", "KB", "MB", "GB", "TB"];
            double tamanho = tamanhoBytes;
            int unidadeIndex = 0;

            while (tamanho >= 1024 && unidadeIndex < unidades.Length - 1)
            {
                tamanho /= 1024;
                unidadeIndex++;
            }

            return $"{tamanho:F2} {unidades[unidadeIndex]}";
        }
        catch (Exception ex)
        {
            return $"Erro ao ler tamanho: {ex.Message}";
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


    /// <summary>
    /// Cria um ToolTip com um design personalizado.
    /// </summary>
    /// <param name="text">O texto a ser exibido no tooltip.</param>
    /// <param name="element">O elemento UI ao qual o tooltip está associado.</param>
    /// <param name="horiOffset">O deslocamento horizontal do tooltip em relação ao elemento.</param>
    /// <param name="vertOffset">O deslocamento vertical do tooltip em relação ao elemento.</param>
    /// <returns>Um objeto ToolTip com o design especificado.</returns>
    public static ToolTip ToolTipDesign(string text, UIElement element, double horiOffset = -55, double vertOffset = -40)
    {
        var tooltip = new ToolTip
        {
            Background = Brushes.Transparent,
            BorderBrush = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            Padding = new Thickness(0),
            PlacementTarget = element,
            Placement = System.Windows.Controls.Primitives.PlacementMode.Relative,
            HorizontalOffset = horiOffset,
            VerticalOffset = vertOffset
        };

        var textoBloco = new TextBlock
        {
            Text = text,
            Foreground = Brushes.White,
            FontSize = 14,
            FontWeight = FontWeights.SemiBold,
            FontFamily = new FontFamily("Segoe UI Variable Display, Bahnschrift"),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };

        var containerEscuro = new Border
        {
            Background = (Brush)new BrushConverter().ConvertFromString("#E60A0A0A")!, // Fundo escuro com opacidade
            BorderBrush = (Brush)new BrushConverter().ConvertFromString("#2D323F")!, // Cor da borda
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(14, 6, 14, 6),
            Child = textoBloco
        };

        tooltip.Content = containerEscuro;
        return tooltip;
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
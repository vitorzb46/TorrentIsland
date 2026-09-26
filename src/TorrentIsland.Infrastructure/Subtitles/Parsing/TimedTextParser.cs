using System.Globalization;
using System.Text;
using TorrentIsland.Infrastructure.Subtitles.Models;

namespace TorrentIsland.Infrastructure.Subtitles.Parsing;

/// <summary>
/// Implementação comum de parsing para formatos de legenda textual cujo timing aparece
/// em uma linha no formato <c>HH:MM:SS.mmm --&gt; HH:MM:SS.mmm</c>.
/// </summary>
/// <remarks>
/// <para>
/// SRT e WebVTT compartilham a mesma estrutura básica — linha de timing seguida de uma
/// ou mais linhas de texto — e diferem apenas em detalhes sintáticos. Este parser
/// parametriza essas diferenças via <c>msSeparator</c> (vírgula vs ponto) e
/// <c>skipIndexLine</c> (presença ou não de índice numérico antes do timing).
/// </para>
/// <para>
/// Quando o payload não contém uma linha de timing válida, o parser usa
/// <see cref="SubtitleCue.Timestamp"/> como <see cref="SubtitleCue.Start"/> e, se
/// disponível, soma <c>blockDuration</c> para calcular <see cref="SubtitleCue.End"/>.
/// Isso cobre o caso de alguns muxers que dividem um cue longo em vários blocos Matroska.
/// </para>
/// </remarks>
internal static class TimedTextParser
{
    /// <summary>
    /// Interpreta o payload de um cue e preenche seus campos de parsing.
    /// </summary>
    /// <param name="cue">Cue com o payload textual em <see cref="SubtitleCue.Data"/>.</param>
    /// <param name="blockDuration">
    /// Duração vinda do elemento <c>BlockDuration</c>, usada como fallback para
    /// <see cref="SubtitleCue.End"/> quando o payload não declara timing.
    /// </param>
    /// <param name="msSeparator">
    /// Separador decimal dos milissegundos. Vírgula (<c>,</c>) para SRT, ponto (<c>.</c>)
    /// para WebVTT.
    /// </param>
    /// <param name="skipIndexLine">
    /// <c>true</c> se o formato puder iniciar o cue com uma linha de índice numérico
    /// (comportamento do SRT). <c>false</c> para WebVTT.
    /// </param>
    /// <returns>
    /// O próprio <paramref name="cue"/>, com <see cref="SubtitleCue.Start"/>,
    /// <see cref="SubtitleCue.End"/> e <see cref="SubtitleCue.Text"/> preenchidos.
    /// </returns>
    public static SubtitleCue Parse(
        SubtitleCue cue,
        TimeSpan? blockDuration,
        char msSeparator,
        bool skipIndexLine)
    {
        var text = Encoding.UTF8.GetString(cue.Data).Trim('\uFEFF', '\r', '\n', ' ');
        if (string.IsNullOrEmpty(text)) return cue;

        var lines = text.Split('\n');
        int startLine = 0;

        // SRT: primeira linha pode ser um índice numérico
        if (skipIndexLine && lines.Length > 1 && int.TryParse(lines[0].Trim(), out _))
            startLine = 1;

        // Tenta extrair a linha de timing
        if (startLine < lines.Length &&
            TryParseTiming(lines[startLine], msSeparator, out var start, out var end))
        {
            cue.Start = start;
            cue.End = end;
            cue.Text = string.Join("\n", lines.Skip(startLine + 1)).Trim();
        }
        else
        {
            // Sem timing no payload: usa o timestamp do Block
            cue.Start = cue.Timestamp;
            cue.End = blockDuration.HasValue
                ? cue.Timestamp + blockDuration.Value
                : cue.Timestamp;
            cue.Text = text;
        }

        return cue;
    }

    /// <summary>
    /// Tenta extrair os instantes de início e fim de uma linha de timing.
    /// </summary>
    /// <param name="line">Linha candidata, por exemplo <c>"00:01:23,456 --&gt; 00:01:25,789"</c>.</param>
    /// <param name="msSeparator">Separador decimal dos milissegundos.</param>
    /// <param name="start">Instante de início decodificado, quando o retorno é <c>true</c>.</param>
    /// <param name="end">Instante de término decodificado, quando o retorno é <c>true</c>.</param>
    /// <returns>
    /// <c>true</c> se a linha contém o separador <c>--&gt;</c> e ambos os timecodes são válidos;
    /// <c>false</c> caso contrário.
    /// </returns>
    private static bool TryParseTiming(
        string line, char msSeparator, out TimeSpan start, out TimeSpan end)
    {
        start = default;
        end = default;

        int idx = line.IndexOf("-->", StringComparison.Ordinal);
        if (idx < 0) return false;

        var left  = line[..idx].Trim();
        var right = line[(idx + 3)..].Trim();

        return TryParseTimecode(left, msSeparator, out start)
            && TryParseTimecode(right, msSeparator, out end);
    }

    /// <summary>
    /// Converte um timecode textual em <see cref="TimeSpan"/>.
    /// </summary>
    /// <param name="s">Timecode no formato <c>HH:MM:SS.mmm</c> ou <c>MM:SS.mmm</c>.</param>
    /// <param name="msSeparator">Separador decimal usado no timecode de entrada.</param>
    /// <param name="result">TimeSpan resultante, quando o retorno é <c>true</c>.</param>
    /// <returns>
    /// <c>true</c> se o timecode pôde ser interpretado; <c>false</c> se o formato for
    /// inesperado ou os componentes não forem numéricos.
    /// </returns>
    /// <remarks>
    /// Aceita tanto <c>HH:MM:SS</c> quanto <c>MM:SS</c>, o que cobre variações comuns
    /// em arquivos malformados. A conversão usa <see cref="CultureInfo.InvariantCulture"/>
    /// para evitar dependência da cultura do sistema.
    /// </remarks>
    private static bool TryParseTimecode(string s, char msSeparator, out TimeSpan result)
    {
        result = default;

        // Normaliza "MM:SS,mmm" ou "HH:MM:SS,mmm" para usar ponto
        var normalized = s.Replace(msSeparator, '.');
        var partes = normalized.Split(':');
        if (partes.Length < 2 || partes.Length > 3) return false;

        try
        {
            int horas = 0, minutos;
            double segundosResto;

            if (partes.Length == 3)
            {
                horas         = int.Parse(partes[0]);
                minutos       = int.Parse(partes[1]);
                segundosResto = double.Parse(partes[2], CultureInfo.InvariantCulture);
            }
            else
            {
                minutos       = int.Parse(partes[0]);
                segundosResto = double.Parse(partes[1], CultureInfo.InvariantCulture);
            }

            int segundos = (int)segundosResto;
            int ms       = (int)Math.Round((segundosResto - segundos) * 1000);

            result = new TimeSpan(0, horas, minutos, segundos, ms);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
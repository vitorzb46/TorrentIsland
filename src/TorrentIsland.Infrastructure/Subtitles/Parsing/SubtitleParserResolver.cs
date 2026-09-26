using TorrentIsland.Infrastructure.Subtitles.Models;

namespace TorrentIsland.Infrastructure.Subtitles.Parsing;

/// <summary>
/// Resolve, a partir do <c>CodecID</c> declarado no <c>TrackEntry</c>, qual parser deve
/// ser usado para interpretar o payload dos cues de uma faixa de legenda.
/// </summary>
/// <remarks>
/// A resolução é feita uma única vez por faixa, e o delegate retornado é reutilizado
/// para todos os cues daquela faixa — evitando a criação repetida de delegates durante
/// a etapa de parsing.
/// </remarks>
internal static class SubtitleParserResolver
{
    /// <summary>
    /// Retorna o parser correspondente ao <c>CodecID</c> informado, ou <c>null</c> se
    /// o codec não for suportado.
    /// </summary>
    /// <param name="codecId">
    /// Identificador do codec, como declarado no <c>TrackEntry</c> do arquivo Matroska
    /// ou WebM. A comparação é case-insensitive.
    /// </param>
    /// <returns>
    /// Delegate com a assinatura <c>(SubtitleCue, TimeSpan?) -&gt; SubtitleCue</c>, ou
    /// <c>null</c> quando o codec não é suportado.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Codecs suportados atualmente:
    /// <list type="bullet">
    ///   <item><description><c>D_WEBVTT/*</c> — qualquer subtipo WebVTT (<c>SUBTITLES</c>, <c>CAPTIONS</c>).</description></item>
    ///   <item><description><c>S_TEXT/UTF8</c> — SRT codificado em UTF-8.</description></item>
    ///   <item><description><c>S_TEXT/ASCII</c> — SRT codificado em ASCII de 7 bits.</description></item>
    ///   <item><description><c>S_UTF8</c> — variante antiga do identificador SRT.</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// O prefixo <c>D_WEBVTT/</c> é verificado antes do <c>switch</c> porque seu formato
    /// não casa com os demais. Os identificadores SRT são normalizados para maiúsculas
    /// via <see cref="string.ToUpperInvariant"/> para comparação insensível a caixa.
    /// </para>
    /// </remarks>
    public static Func<SubtitleCue, TimeSpan?, SubtitleCue>? Resolve(string codecId)
    {
        if (string.IsNullOrEmpty(codecId)) return null;

        // WebM: D_WEBVTT/SUBTITLES, D_WEBVTT/CAPTIONS, ...
        if (codecId.StartsWith("D_WEBVTT/", StringComparison.OrdinalIgnoreCase))
            return WebVttParser.Parse;

        // MKV: S_TEXT/UTF8 (SRT), S_TEXT/ASCII (SRT 7-bit), S_UTF8 (variante antiga)
        return codecId.ToUpperInvariant() switch
        {
            "S_TEXT/UTF8"  => SrtParser.Parse,
            "S_TEXT/ASCII" => SrtParser.Parse,
            "S_UTF8"       => SrtParser.Parse,
            _              => null
        };
    }
}
using TorrentIsland.Infrastructure.Subtitles.Models;

namespace TorrentIsland.Infrastructure.Subtitles.Parsing;

/// <summary>
/// Parser de cues no formato WebVTT (Web Video Text Tracks).
/// </summary>
/// <remarks>
/// Delega para <see cref="TimedTextParser"/> com as particularidades do WebVTT: separador
/// decimal é ponto (<c>.</c>) e o formato não usa índices numéricos antes do timing.
/// </remarks>
internal static class WebVttParser
{
    /// <summary>
    /// Interpreta o payload bruto de um cue WebVTT, preenchendo <see cref="SubtitleCue.Start"/>,
    /// <see cref="SubtitleCue.End"/> e <see cref="SubtitleCue.Text"/>.
    /// </summary>
    /// <param name="cue">Cue com o payload WebVTT em <see cref="SubtitleCue.Data"/>.</param>
    /// <param name="blockDuration">
    /// Duração vinda de <c>BlockDuration</c>, usada como fallback quando o payload não
    /// contém linha de timing.
    /// </param>
    /// <returns>O próprio <paramref name="cue"/>, com os campos de parsing preenchidos.</returns>
    public static SubtitleCue Parse(SubtitleCue cue, TimeSpan? blockDuration)
        => TimedTextParser.Parse(cue, blockDuration, msSeparator: '.', skipIndexLine: false);
}
using TorrentIsland.Infrastructure.Subtitles.Models;

namespace TorrentIsland.Infrastructure.Subtitles.Parsing;

/// <summary>
/// Parser de cues no formato SRT (SubRip Text).
/// </summary>
/// <remarks>
/// Delega para <see cref="TimedTextParser"/> com as particularidades do SRT: separador
/// decimal é vírgula (<c>,</c>) e cada cue pode ser precedido por uma linha de índice
/// numérico.
/// </remarks>
internal static class SrtParser
{
    /// <summary>
    /// Interpreta o payload bruto de um cue SRT, preenchendo <see cref="SubtitleCue.Start"/>,
    /// <see cref="SubtitleCue.End"/> e <see cref="SubtitleCue.Text"/>.
    /// </summary>
    /// <param name="cue">Cue com o payload SRT em <see cref="SubtitleCue.Data"/>.</param>
    /// <param name="blockDuration">
    /// Duração vinda de <c>BlockDuration</c>, usada como fallback quando o payload não
    /// contém linha de timing.
    /// </param>
    /// <returns>O próprio <paramref name="cue"/>, com os campos de parsing preenchidos.</returns>
    public static SubtitleCue Parse(SubtitleCue cue, TimeSpan? blockDuration)
        => TimedTextParser.Parse(cue, blockDuration, msSeparator: ',', skipIndexLine: true);
}
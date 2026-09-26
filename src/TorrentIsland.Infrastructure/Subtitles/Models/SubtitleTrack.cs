namespace TorrentIsland.Infrastructure.Subtitles.Models;

/// <summary>
/// Representa uma faixa de legenda completa: metadados do <c>TrackEntry</c> e a lista
/// de cues extraídos.
/// </summary>
public sealed class SubtitleTrack
{
    public ulong TrackNumber { get; init; }
    public string CodecId { get; init; } = string.Empty;
    public string Language { get; init; } = "und";
    public string Name { get; init; } = string.Empty;
    public List<SubtitleCue> Cues { get; init; } = [];
}

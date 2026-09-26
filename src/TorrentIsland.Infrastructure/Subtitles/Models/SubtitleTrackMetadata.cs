namespace TorrentIsland.Infrastructure.Subtitles.Models;

/// <summary>
/// Metadados de uma faixa de legenda lidos do <c>TrackEntry</c>, antes de qualquer cue
/// ser extraído.
/// </summary>
public sealed class SubtitleTrackMetadata
{
    public ulong TrackNumber { get; init; }
    public string CodecId { get; init; } = string.Empty;
    public string Language { get; init; } = "und";
    public string Name { get; init; } = string.Empty;
    public byte[] CodecPrivate { get; init; } = [];
}

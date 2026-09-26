namespace TorrentIsland.Infrastructure.Subtitles.Models;

/// <summary>
/// Representa um único cue de uma faixa de legenda, com seu
/// payload bruto e os campos já parseados.
/// </summary>
public sealed class SubtitleCue
{
    public ulong TrackNumber { get; init; }
    public TimeSpan Timestamp { get; init; }
    public TimeSpan? Duration { get; init; }
    public byte[] Data { get; init; } = [];
    // Parse
    public TimeSpan? Start { get; set; }
    public TimeSpan? End { get; set; }
    public string? Text { get; set; }
}

namespace TorrentIsland.Domain.DTOs;

public record TorrentCreationInfo(
    string MagnetLink,
    string SavePath,
    IReadOnlyList<string>? Trackers = null
);

using LibVLCSharp.Shared;

namespace TorrentIsland.Presentation.Player.TvCast;

public sealed class CastDevice
{
    public string Name { get; set; } = string.Empty;
    public RendererItem? Item { get; set; }
}

using MonoTorrent.Client;
using TorrentIsland.Domain.Enums;

namespace TorrentIsland.Infrastructure.MonoTorrent;

public static class TorrentManagerExtensions
{
    public static TorrentEstado Estado(this TorrentManager manager)
    {
        return manager.State switch
        {
            TorrentState.Error => TorrentEstado.Erro,
            TorrentState.Seeding => TorrentEstado.Semeando,
            TorrentState.Stopped => TorrentEstado.Parado,
            TorrentState.Downloading => TorrentEstado.Baixando,
            TorrentState.Paused => TorrentEstado.Pausado,
            TorrentState.FetchingHashes => TorrentEstado.BuscandoHashs,
            TorrentState.Hashing => TorrentEstado.VerificandoHash,
            TorrentState.HashingPaused => TorrentEstado.HashPausado,
            TorrentState.Starting => TorrentEstado.Iniciando,
            TorrentState.Stopping => TorrentEstado.Parando,
            TorrentState.Metadata => TorrentEstado.Metadata,
            _ when manager.Progress >= 100d => TorrentEstado.Concluido,
            _ => TorrentEstado.Erro,
        };
    }
}

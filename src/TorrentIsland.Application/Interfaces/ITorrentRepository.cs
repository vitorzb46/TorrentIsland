using TorrentIsland.Application.DTOs;
using TorrentIsland.Domain.Entities;
using TorrentIsland.Domain.Enums;

namespace TorrentIsland.Application.Interfaces;

public interface ITorrentRepository
{
    /// <summary>
    /// Inicia o Engine. Se o magnet link for fornecido, parseia e adiciona o torrent ao motor.
    /// Se o nome da pasta for fornecido, adiciona os torrents na pasta ao motor.
    /// </summary>
    /// <param name="magnetOrFolderName">Magnet link ou nome da pasta.</param>
    /// <returns>A unique identifier for the added engine.</returns>
    /// <returns>Um identificador único para o torrent.</returns>
    Task<List<Guid>> AddEngineAsync(string magnetOrFolderName);
    IReadOnlyList<(Guid Id, string Nome, TorrentEstado Estado, int Seeds, int Peers)> EstadoDosTorrents();
    /// <summary>
    /// Obtém o torrent pelo id com as propriedades de progresso/velocidade/seeds preenchidas.
    /// </summary>
    Task<TorrentEntity?> ObterAsync(Guid id);
    Task<IReadOnlyDictionary<Guid, TorrentDto>> ObterManagersAsync();
    Task StartAllTorrentAsync();
    Task StartTorrentAsync(Guid id);
    Task TrackersAsync(Guid id);
    Task EventsAsync(Guid id);
}

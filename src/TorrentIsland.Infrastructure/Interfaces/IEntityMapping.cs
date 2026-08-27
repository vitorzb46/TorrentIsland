using MonoTorrent.Client;
using TorrentIsland.Application.DTOs;
using TorrentIsland.Domain.Entities;
using TorrentIsland.Infrastructure.DTOs;

namespace TorrentIsland.Infrastructure.Interfaces
{
    public interface IEntityMapping
    {
        TorrentEntity ToEntity(TorrentDadosBrutos dados);
    }
}
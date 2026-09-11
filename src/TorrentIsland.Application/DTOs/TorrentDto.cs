using TorrentIsland.Domain.Entities;
using TorrentIsland.Domain.Enums;

namespace TorrentIsland.Application.DTOs;

public class TorrentDto
{
    #region Properties Entity
    public Guid Id { get; init; }
    public string? Nome { get; init; }
    public TorrentEstado Estado { get; init; }
    public double Progresso { get; init; }
    public long TamanhoTotal { get; init; }
    public long BytesRecebidos { get; init; }
    public long BytesRestantes { get => TamanhoTotal - BytesRecebidos; }
    public long VelocidadeDownload { get; init; }
    public long VelocidadeUpload { get; init; }
    public int Seeds { get; init; }
    public int ParesDisponiveis { get; init; }
    public IReadOnlyList<string> Trackers { get; init; } = [];
    public string SavePath { get; init; } = string.Empty;
    public string FullPath { get; init; } = string.Empty;
    public string? TempoEstimado { get; init; }
    public string? CorEstado { get; init; }
    public TimeSpan TempoTotal { get; set; }
    #endregion
    public static TorrentDto FromEntity(TorrentEntity entity)
    {
        return new TorrentDto
        {
            Id = entity.Id,
            Nome = entity.Nome,
            Estado = entity.Estado,
            Progresso = entity.Progresso,
            TamanhoTotal = entity.TamanhoTotal,
            BytesRecebidos = entity.BytesRecebidos,
            VelocidadeDownload = entity.VelocidadeDownload,
            VelocidadeUpload = entity.VelocidadeUpload,
            Seeds = entity.Seeds,
            ParesDisponiveis = entity.ParesDisponiveis,
            Trackers = [.. entity.Trackers],
            SavePath = entity.SavePath,
            FullPath = entity.FullPath,
            TempoEstimado = entity.TempoEstimado,
            CorEstado = entity.CorEstado
        };
    }
}

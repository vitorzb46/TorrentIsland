using TorrentIsland.Domain.Enums;

namespace TorrentIsland.Infrastructure.DTOs;

public record TorrentDadosBrutos(Guid Id,
                                 string Nome,
                                 long TamanhoTotal,
                                 List<string> Trackers,
                                 string SavePath,
                                 TorrentEstado Estado,
                                 double Progresso,
                                 long BytesRecebidos,
                                 long BytesRestantes,
                                 long VelocidadeDownload,
                                 long VelocidadeUpload,
                                 int Seeds,
                                 int ParesDisponiveis);

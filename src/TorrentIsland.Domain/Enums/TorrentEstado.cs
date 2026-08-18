namespace TorrentIsland.Domain.Enums;

public enum TorrentEstado
{
    Aguardando,
    Baixando,
    Pausado,
    Parado,
    Erro,
    Semeando,
    BuscandoHashs,
    VerificandoHash,
    HashPausado,
    Iniciando,
    Parando,
    Metadata,
    Concluido
}

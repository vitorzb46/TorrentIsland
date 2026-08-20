using TorrentIsland.Domain.Enums;

namespace TorrentIsland.Domain.Entities;

public class TorrentEntity
{
    #region Entity
    public Guid Id { get; private set; }
    public string? Nome { get; private set; }
    public long TamanhoTotal { get; private set; }
    public IList<string> Trackers { get; private set; } = [];
    public string SavePath { get; private set; } = string.Empty;
    public TorrentEstado Estado { get; private set; }
    public double Progresso { get; private set; }
    public long BytesRecebidos { get; private set; }
    public long BytesRestantes { get => TamanhoTotal - BytesRecebidos; }
    public long VelocidadeDownload { get; private set; }
    public long VelocidadeUpload { get; private set; }
    public int Seeds { get; private set; }
    public int ParesDisponiveis { get; private set; }
    public string? TempoEstimado { get; private set; }
    public string? CorEstado { get; private set; }
    #endregion

    // Métodos para modificar o estado
    public void SetId(Guid id) => Id = id;
    public void SetNome(string? nome) => Nome = nome;
    public void SetTamanhoTotal(long tamanhoTotal) => TamanhoTotal = tamanhoTotal;
    public void SetTrackers(IList<string> trackers) => Trackers = trackers;
    public void SetSavePath(string savePath) => SavePath = savePath;
    public void SetEstado(TorrentEstado estado) => Estado = estado;
    public void SetProgresso(double progresso) => Progresso = progresso;
    public void SetBytesRecebidos(long bytesRecebidos) => BytesRecebidos = bytesRecebidos;
    public void SetVelocidadeDownload(long velocidadeDownload) => VelocidadeDownload = velocidadeDownload;
    public void SetVelocidadeUpload(long velocidadeUpload) => VelocidadeUpload = velocidadeUpload;
    public void SetSeeds(int seeds) => Seeds = seeds;
    public void SetParesDisponiveis(int paresDisponiveis) => ParesDisponiveis = paresDisponiveis;
    public void SetTempoEstimado(string? tempoEstimado) => TempoEstimado = tempoEstimado;
    public void SetCorEstado() => CorEstado = Cor();



    private string Cor()
    {
        return Estado switch
        {
            TorrentEstado.Concluido or TorrentEstado.Semeando => Verde,
            TorrentEstado.Erro => Vermelho,
            _ => Amarelo
        };
    }

    private string Verde { get; } = "[green]";
    private string Vermelho { get; } = "[red]";
    private string Amarelo { get; } = "[yellow]";
}

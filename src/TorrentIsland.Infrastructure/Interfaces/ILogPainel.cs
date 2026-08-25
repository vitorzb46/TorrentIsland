namespace TorrentIsland.Infrastructure.Interfaces;

public interface ILogPainel
{
    void Adicionar(string mensagem);
    string Conteudo();
    void Limpar();
}

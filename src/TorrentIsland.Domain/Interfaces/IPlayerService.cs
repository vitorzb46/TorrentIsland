namespace TorrentIsland.Domain.Interfaces;

public interface IPlayerService : IDisposable
{
    void Reproduzir(Uri uri);
    void Pausar();
    void Parar();
    bool EstaReproduzindo { get; }
}

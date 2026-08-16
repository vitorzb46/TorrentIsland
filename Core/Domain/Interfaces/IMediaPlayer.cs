namespace Core.Domain.Interfaces
{
    public interface IMediaPlayer
    {
        void InicializarPlayer(object PlayerView);
        void Play(Stream StreamMedia);
        void Play(string FilePath);
        void Pause();
        void Stop();
    }
}

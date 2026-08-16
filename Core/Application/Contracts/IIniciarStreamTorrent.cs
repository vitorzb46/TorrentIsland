public interface IIniciarStreamTorrent
{
    Task<Stream> StreamAsync(string magnetLink);
}
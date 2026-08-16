namespace Application.Contracts
{
    public interface IIniciarDownload
    {
        Task<Guid> ExecutarAsync(string url);
    }
}
namespace Core.Domain.Entities
{
    public class DownloadItem
    {
        public Guid Id { get; set; }
        public string? Titulo { get; set; }
        public double Progresso { get; set; } // 0 a 100
        public string? Status { get; set; }
    }
}

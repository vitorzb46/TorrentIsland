using System.Collections.Concurrent;
using TorrentIsland.Application.Interfaces;
using TorrentIsland.Application.Settings;

namespace TorrentIsland.Infrastructure.Services;

public partial class SubCacheManager
{
    private static readonly string CacheFile = AppSettings.SubCacheFile;

    private static readonly ConcurrentDictionary<string, CacheEntry> _cache = new();

    static SubCacheManager()
    {
        _ = Load();
    }

    /// <summary>
    /// Recupera de forma assíncrona uma entrada do cache e valida sua integridade contra o arquivo em disco.
    /// </summary>
    /// <param name="caminhoDoVideo">O caminho físico do arquivo de vídeo no disco usado para validar o cache atual.</param>
    /// <param name="trackId">O identificador único da faixa de legenda do vídeo.</param>
    /// <returns>
    /// Retorna o <see cref="CacheEntry"/> correspondente se o cache for válido; 
    /// caso contrário, remove a entrada inválida do cache, persiste a alteração via
    /// <see cref="Save"/> e retorna <see langword="null"/>.
    /// </returns>
    public static async Task<CacheEntry?> GetAsync(string caminhoDoVideo, int trackId)
    {
        var key = MakeKey(caminhoDoVideo, trackId);
        _cache.TryGetValue(key, out var entry);
        if (!IsValid(caminhoDoVideo, entry!))
        {
            _cache.TryRemove(key, out _);
            await JsonFiles.SaveToFileAsync(_cache, CacheFile);
            return null;
        }
        return entry;
    }

    /// <summary>
    /// Cria um novo objeto de <see cref="CacheEntry"/> com a entrada de dados da legenda e metadados do arquivo de vídeo.
    /// </summary>
    /// <param name="caminhoDoVideo">O caminho físico do arquivo de vídeo no disco para extração de tamanho e data de modificação.</param>
    /// <param name="trackId">O identificador único da faixa de legenda do vídeo.</param>
    /// <param name="language">O idioma correspondente à legenda que está sendo salva em ISO 2 letras.</param>
    /// <remarks>
    /// Inclui a chave hash gerada pelo <see cref="KeyGenerator"/> e persiste alteração via <see cref="Save"/>.
    /// </remarks>
    public static async Task SetAsync(string caminhoDoVideo, int trackId, string language)
    {
        var entry = new CacheEntry
        {
            Hash = KeyGenerator.HashString,
            Language = language,
            FileSize = new FileInfo(caminhoDoVideo).Length,
            LastWriteTime = File.GetLastWriteTime(caminhoDoVideo),
            DetectionTime = DateTime.Now
        };
        _cache[MakeKey(caminhoDoVideo, trackId)] = entry;
        await JsonFiles.SaveToFileAsync(_cache, CacheFile);
    }

    public static async Task Load()
    {
        await JsonFiles.LoadFromFileAsync(_cache, CacheFile);
    }

    /// <summary>Pega <paramref name="videoPath"/> e <paramref name="trackId"/>, transformando os 2 em uma Key</summary>
    private static string MakeKey(string videoPath, int trackId) => $"{videoPath}|{trackId}";

    /// <summary>
    /// Realiza a validação do cache em disco comparando os metadados do arquivo atual
    /// com o <see cref="CacheEntry"/> fornecido.
    /// </summary>
    /// <param name="videoPath">O caminho físico do arquivo de vídeo no disco para verificação.</param>
    /// <param name="entry">O objeto contendo os metadados salvos (tamanho, data de modificação e hash) a serem validados.</param>
    /// <returns>Retorna <see langword="true"/> se o tamanho,
    /// a data da última escrita e o hash forem idênticos; caso contrário, <see langword="false"/>.</returns>
    private static bool IsValid(string videoPath, CacheEntry entry)
    {
        if (entry == null) return false;
        if (!File.Exists(videoPath)) return false;
        var fi = new FileInfo(videoPath);
        return fi.Length == entry.FileSize
               && KeyGenerator.HashString == entry.Hash;
    }
}
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using TorrentIsland.Infrastructure.Logging;

namespace TorrentIsland.Infrastructure.Services;

public class KeyGenerator
{
    public static string HashString { get; private set; } = string.Empty;
    /// <summary>
    /// Lê o header de memória de um arquivo de mídia.
    /// </summary>
    /// <param name="filePath">caminho de arquivo de mídia.</param>
    /// <returns>Retorna um SHA256 em formato string.</returns>
    public static string Hash(string filePath)
    {
        if (HashString != string.Empty) return HashString;

        var info = new FileInfo(filePath);

        if (!info.Exists) return string.Empty;

        var bufferOriginal = new byte[128 * 1024];
        try
        {
            using (var stream = new FileStream(filePath,
                                               FileMode.Open,
                                               FileAccess.Read,
                                               FileShare.ReadWrite))
            {
                _ = stream.Read(bufferOriginal, 0, bufferOriginal.Length);
            }

            var fileName = Path.GetFileNameWithoutExtension(filePath);
            var size = info.Length;
            var bytesId = Encoding.UTF8.GetBytes($"{fileName}_{size}");
            var finalBytes = new byte[bytesId.Length + bufferOriginal.Length];
    
            Buffer.BlockCopy(bytesId, 0, finalBytes, 0, bytesId.Length);
            Buffer.BlockCopy(bufferOriginal, 0, finalBytes, bytesId.Length, bufferOriginal.Length);
            var hashBytes = SHA256.HashData(finalBytes);
    
            return HashString = Convert.ToHexString(hashBytes).ToLower();
        }
        catch (Exception ex)
        {
            Log.Salvar($"Falha ao criar hash: {ex.Message} {ex.StackTrace}");
            return string.Empty;
        }
    }
}
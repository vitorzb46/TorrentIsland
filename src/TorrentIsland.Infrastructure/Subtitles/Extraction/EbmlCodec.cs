using System.Text;

namespace TorrentIsland.Infrastructure.Subtitles.Extraction;

/// <summary>
/// Fornece primitivas de codificação e decodificação EBML usadas pelo
/// <see cref="SubtitleExtractor"/> para interpretar elementos Matroska/WebM.
/// </summary>
/// <remarks>
/// Esta classe não interpreta a semântica dos elementos EBML — apenas converte bytes
/// para os tipos primitivos. A interpretação fica a cargo do chamador.
/// </remarks>
internal static class EbmlCodec
{
    /// <summary>
    /// Lê um inteiro de tamanho variável (VINT) a partir do offset indicado.
    /// </summary>
    /// <param name="data">Buffer contendo os bytes codificados.</param>
    /// <param name="offset">Posição inicial do VINT dentro de <paramref name="data"/>.</param>
    /// <returns>
    /// Uma tupla com o valor decodificado (<c>value</c>) e o número de bytes consumidos
    /// (<c>bytesRead</c>).
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="offset"/> é maior ou igual ao comprimento de <paramref name="data"/>.
    /// </exception>
    /// <exception cref="InvalidDataException">
    /// O VINT tem comprimento maior que 8 bytes ou ultrapassa o fim do buffer.
    /// </exception>
    public static (ulong value, int bytesRead) ReadVint(byte[] data, int offset)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(offset, data.Length);

        byte first = data[offset];
        int length = 1;
        for (int i = 7; i >= 0; i--)
        {
            if ((first & (1 << i)) != 0) break;
            length++;
        }

        if (length > 8 || offset + length > data.Length)
            throw new InvalidDataException($"VINT inválido no offset {offset}.");

        ulong value = (ulong)(first & (0xFF >> length));
        for (int i = 1; i < length; i++)
        {
            value = (value << 8) | data[offset + i];
        }

        return (value, length);
    }

    /// <summary>
    /// Converte o conteúdo de um elemento EBML numérico em <see cref="ulong"/>.
    /// </summary>
    /// <param name="bytes">
    /// Bytes do elemento, tal como retornado por <c>ElementDataToBytes()</c>. Pode ser
    /// <c>null</c> ou vazio.
    /// </param>
    /// <returns>
    /// O valor interpretado como inteiro big-endian sem sinal, ou <c>0</c> quando
    /// <paramref name="bytes"/> é <c>null</c> ou vazio.
    /// </returns>
    /// <remarks>
    /// O Matroska armazena inteiros em big-endian com comprimento variável (1 a 8 bytes),
    /// de modo que <c>0x00 0x00 0x02</c> e <c>0x02</c> representam o mesmo valor.
    /// Este método faz a leitura byte a byte para suportar qualquer comprimento sem
    /// depender da ordem de bytes da plataforma.
    /// </remarks>
    public static ulong ToUInt64(byte[]? bytes)
    {
        if (bytes == null || bytes.Length == 0) return 0UL;

        ulong result = 0;
        foreach (var b in bytes) result = (result << 8) | b;
        return result;
    }

    /// <summary>
    /// Converte o conteúdo de um elemento EBML textual em <see cref="string"/> UTF-8.
    /// </summary>
    /// <param name="bytes">
    /// Bytes do elemento. Pode ser <c>null</c> ou vazio.
    /// </param>
    /// <returns>
    /// A string UTF-8 decodificada, ou <see cref="string.Empty"/> quando
    /// <paramref name="bytes"/> é <c>null</c> ou vazio.
    /// </returns>
    /// <remarks>
    /// O Matroska especifica codificação UTF-8 para elementos de texto. Este método não
    /// valida a sequência UTF-8 — bytes inválidos são substituídos pelo caractere de
    /// substituição (<c>U+FFFD</c>) pelo próprio <see cref="Encoding.UTF8"/>.
    /// </remarks>
    public static string ToString(byte[]? bytes)
    {
        if (bytes == null || bytes.Length == 0) return string.Empty;
        return Encoding.UTF8.GetString(bytes);
    }
}

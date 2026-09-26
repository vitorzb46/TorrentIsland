using SpawnDev.EBML;
using SpawnDev.EBML.Elements;
using SpawnDev.EBML.Schemas;
using TorrentIsland.Infrastructure.Logging;
using TorrentIsland.Infrastructure.Subtitles.Models;
using TorrentIsland.Infrastructure.Subtitles.Parsing;

namespace TorrentIsland.Infrastructure.Subtitles.Extraction;

/// <summary>
/// Extrai faixas de legenda de arquivos de container Matroska (<c>.mkv</c>) e WebM (<c>.webm</c>).
/// </summary>
/// <remarks>
/// <para>
/// A extração é feita em duas etapas: primeiro os metadados das faixas são lidos
/// para descobrir quais são legendas
/// e qual o codec de cada uma; em seguida os clusters são percorridos para acumular os
/// <c>Block</c>/<c>SimpleBlock</c> que pertencem a essas faixas.
/// </para>
/// <para>
/// Suporta os codecs de legenda textual <c>S_TEXT/UTF8</c> (SRT), <c>S_TEXT/ASCII</c> e
/// <c>D_WEBVTT/*</c>.
/// </para>
/// <para>
/// Esta classe realiza I/O síncrono.
/// </para>
/// </remarks>
public sealed class SubtitleExtractor
{

    /// <summary>
    /// Lê apenas o cabeçalho do arquivo Matroska/WebM e retorna os metadados das faixas
    /// de legenda.
    /// </summary>
    /// <param name="filePath">Caminho completo do arquivo MKV/WebM a ser inspecionado.</param>
    /// <returns>
    /// Lista somente-leitura de <see cref="SubtitleTrackMetadata"/> ordenada por
    /// <see cref="SubtitleTrackMetadata.TrackNumber"/>. Retorna lista vazia se o
    /// arquivo não contiver faixas de legenda.
    /// </returns>
    /// <exception cref="FileNotFoundException">Arquivo não encontrado no caminho informado.</exception>
    /// <exception cref="InvalidDataException">Falha ao parsear o documento EBML.</exception>
    public static IReadOnlyList<SubtitleTrackMetadata> GetSubtitleTracksMetadata(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException("Arquivo não encontrado.", filePath);
            
        var parser = new EBMLParser();
        using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        var document = parser.ParseDocument(stream)
                    ?? throw new InvalidDataException("Falha ao parsear o arquivo EBML.");

        var (porNumero, ordenados) = GetMetadata(document);
        return ordenados;
    }
    
    /// <summary>
    /// Extrai cues de legendas de um arquivo Matroska/WebM, opcionalmente
    /// restringindo a extração a um conjunto específico de faixas.
    /// </summary> 
    /// <param name="filePath">Caminho completo do arquivo MKV/WebM.</param>
    /// <param name="trackNumbersFilter">
    /// Conjunto de TrackNumbers (numeração 1-based do Matroska) a extrair.
    /// Se <c>null</c>, todas as faixas de legenda são processadas.
    /// </param>
    /// <param name="maxCuesPerTrack">
    /// Número máximo de cues acumulados por faixa.
    /// </param>
    /// <param name="maxClusters">
    /// Número máximo de Clusters varridos antes de encerrar.
    /// </param>
    /// <returns>
    /// Lista somente-leitura de <see cref="SubtitleTrack"/>, uma por faixa de
    /// legenda suportada por <see cref="SubtitleParserResolver"/>. Faixas sem
    /// cues extraídos aparecem com <see cref="SubtitleTrack.Cues"/> vazio.
    /// </returns>
    /// <exception cref="FileNotFoundException">Arquivo não encontrado no caminho informado.</exception>
    /// <exception cref="InvalidDataException">Falha ao parsear o documento EBML.</exception>
    public static IReadOnlyList<SubtitleTrack> ExtractSubtitles(
        string filePath,
        IReadOnlySet<ulong>? trackNumbersFilter = null,
        int maxCuesPerTrack = 15,
        int maxClusters = 10)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException("Arquivo não encontrado.", filePath);
            
        var parser = new EBMLParser();
        using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        var document = parser.ParseDocument(stream)
                    ?? throw new InvalidDataException("Falha ao parsear o arquivo EBML.");

        var (tracksPorNumero, tracksOrdenados) = GetMetadata(document);
        if (tracksPorNumero.Count == 0) return [];

        var cuesPorTrack = new Dictionary<ulong, List<SubtitleCue>>();
        var clusters = document!.FindMasters("/Segment/Cluster").ToList();

        
        int processados = 0;
        foreach (var cluster in clusters)
        {
            GetCuesFromCluster(cluster, trackNumbersFilter, tracksPorNumero, cuesPorTrack, maxCuesPerTrack);
            processados++;

            if (processados >= maxClusters)
            {
                break;
            }

            if (cuesPorTrack.Count > 0 &&
                cuesPorTrack.Values.All(l => l.Count >= maxCuesPerTrack))
            {
                break;
            }
        }

        var resultado = new List<SubtitleTrack>(tracksPorNumero.Count);
        foreach (var meta in tracksPorNumero.Values)
        {
            var parse = SubtitleParserResolver.Resolve(meta.CodecId);
            if (parse is null) continue;

            var track = new SubtitleTrack
            {
                TrackNumber = meta.TrackNumber,
                CodecId     = meta.CodecId,
                Language    = meta.Language,
                Name        = meta.Name
            };

            if (cuesPorTrack.TryGetValue(meta.TrackNumber, out var cues))
            {
                foreach (var cue in cues)
                {
                    parse(cue, cue.Duration);
                }
                track.Cues.AddRange(cues);
            }
            resultado.Add(track);
        }
        
        // === DIAGNÓSTICO: resumo por track ===
        Log.Salvar($"[Diag] === Resumo da extração ===");
        Log.Salvar($"[Diag] Tracks de legenda descobertas: {tracksPorNumero.Count}");
        Log.Salvar($"[Diag] Total de cues extraídos: {resultado.Sum(t => t.Cues.Count)}");

        foreach (var track in resultado.OrderBy(t => t.TrackNumber))
        {
            var cues = track.Cues;
            if (cues.Count == 0)
            {
                Log.Salvar($"[Diag] Track #{track.TrackNumber}: 0 cues");
                continue;
            }

            var primeiro = cues.Min(c => c.Timestamp);
            var ultimo   = cues.Max(c => c.Timestamp);

            Log.Salvar($"[Diag] Track #{track.TrackNumber}: {cues.Count} cues | " +
                    $"primeiro={primeiro:mm\\:ss\\.fff} último={ultimo:mm\\:ss\\.fff}");
        }
        return resultado;
    }

    /// <summary>
    /// Resolve as faixas de legenda do documento EBML em duas representações:
    /// um dicionário indexado por TrackNumber e uma lista ordenada.
    /// </summary>
    /// <remarks>
    /// Percorre <c>Segment</c> (<c>0x18538067</c>) → <c>Tracks</c>
    /// (<c>0x1654AE6B</c>) → <c>TrackEntry</c> (<c>0xAE</c>) usando
    /// <c>Children</c> + <c>OfType&lt;MasterElement&gt;()</c>
    /// <para>
    /// Somente entradas com <c>TrackType == 0x11</c> (subtitles) são retornadas.
    /// CodecPrivate é preservado para parsers que precisam dele (ex.: WebVTT
    /// dentro de MKV).
    /// </para>
    /// </remarks>
    /// <param name="document">Documento EBML já parseado.</param>
    /// <returns>
    /// Uma tupla com:
    /// <list type="bullet">
    /// <item><c>porNumero</c>: dicionário <c>TrackNumber → metadados</c> para
    /// busca O(1) durante a varredura de Clusters.</item>
    /// <item><c>ordenados</c>: lista ordenada por TrackNumber, para
    /// pareamento posicional com as faixas do VLC.</item>
    /// </list>
    /// </returns>
    private static
        (Dictionary<ulong, SubtitleTrackMetadata> porNumero, List<SubtitleTrackMetadata> ordenados)
            GetMetadata(MasterElement document)
    {
        var porNumero = new Dictionary<ulong, SubtitleTrackMetadata>();
        var ordenados = new List<SubtitleTrackMetadata>();

        var resultado = new Dictionary<ulong, SubtitleTrackMetadata>();

        foreach (var segment in document.Children
                                    .Where(c => c.Id == 0x18538067)
                                    .OfType<MasterElement>())
        foreach (var tracks in segment.Children
                                    .Where(c => c.Id == 0x1654AE6B)
                                    .OfType<MasterElement>())
        foreach (var entry in tracks.Children
                                    .Where(c => c.Id == 0xAE)
                                    .OfType<MasterElement>())
        {
            byte[]? trackTypeRaw = null, trackNumberRaw = null;
            string codecId = "", language = "", name = "";
            byte[] codecPrivate = [];

            foreach (var field in entry.Children)
            {
                switch (field.Id)
                {
                    case 0x83:     trackTypeRaw   = field.ElementDataToBytes(); break;
                    case 0xD7:     trackNumberRaw = field.ElementDataToBytes(); break;
                    case 0x86:     codecId        = EbmlCodec.ToString(field.ElementDataToBytes()); break;
                    case 0x22B59C: language       = EbmlCodec.ToString(field.ElementDataToBytes()); break;
                    case 0x536E:   name           = EbmlCodec.ToString(field.ElementDataToBytes()); break;
                    case 0x63A2:   codecPrivate   = field.ElementDataToBytes(); break;
                }
            }

            if (trackTypeRaw is null || trackNumberRaw is null) continue;
            if (EbmlCodec.ToUInt64(trackTypeRaw) != 0x11) continue;

            var number = EbmlCodec.ToUInt64(trackNumberRaw);
            var meta = new SubtitleTrackMetadata
            {
                TrackNumber  = number,
                CodecId      = codecId,
                Language     = string.IsNullOrEmpty(language) ? "und" : language,
                Name         = name,
                CodecPrivate = codecPrivate
            };

            porNumero[number] = meta;
            ordenados.Add(meta);
        }

        // Garante ordem crescente de TrackNumber (ordem do arquivo)
        ordenados.Sort((a, b) => a.TrackNumber.CompareTo(b.TrackNumber));
        return (porNumero, ordenados);
    }
    
    /// <summary>
    /// Percorre os filhos de um Cluster e acumula cues das faixas de legenda
    /// desejadas em <paramref name="cuesPorTrack"/>.
    /// </summary>
    /// <remarks>
    /// Fluxo por Cluster:
    /// <list type="number">
    /// <item>Lê o <c>Timestamp</c> (<c>0xE7</c>) do Cluster para uso como offset
    /// absoluto de todos os cues do cluster.</item>
    /// <item>Ignora <c>SimpleBlock</c> (<c>0xA3</c>).</item>
    /// <item>Para cada <c>BlockGroup</c> (<c>0xA0</c>), lê o <c>Block</c>
    /// (<c>0xA1</c>) e o <c>BlockDuration</c> (<c>0x9B</c>) opcional.</item>
    /// <item>Se <paramref name="trackNumbersFilter"/> estiver definido, faz um
    /// <c>peek</c> do TrackNumber VINT antes de ler o payload, descartando cedo
    /// as faixas não solicitadas.</item>
    /// <item>Blocos com <c>DataSize</c> fora de <c>[4, 4096]</c> são ignorados.</item>
    /// </list>
    /// </remarks>
    /// <param name="cluster">Cluster EBML a processar.</param>
    /// <param name="trackNumbersFilter">
    /// Conjunto de TrackNumbers a considerar. <c>null</c> processa todas as
    /// faixas presentes em <paramref name="tracks"/>.
    /// </param>
    /// <param name="tracks">Dicionário de metadados indexado por TrackNumber,
    /// usado para validar se um bloco pertence a uma faixa de legenda conhecida.</param>
    /// <param name="cuesPorTrack">Acumulador de cues por TrackNumber. Criado e
    /// mantido pelo chamador ao longo de todos os Clusters.</param>
    /// <param name="maxCuesPerTrack">Limite de cues por faixa; ao atingir, novos
    /// cues daquela faixa são descartados por <c>AddCue</c>.</param>
    private static void GetCuesFromCluster(
        MasterElement cluster,
        IReadOnlySet<ulong>? trackNumbersFilter,
        Dictionary<ulong, SubtitleTrackMetadata> tracks,    
        Dictionary<ulong, List<SubtitleCue>> cuesPorTrack,
        int maxCuesPerTrack)
    {
        var peekBuffer = new byte[8];
        TimeSpan clusterTimestamp = TimeSpan.Zero;

        foreach (var child in cluster.Children)
        {
            if (child.Id == 0xE7) // Timestamp
            {
                var raw = child.ElementDataToBytes();
                if (raw != null)
                    clusterTimestamp = TimeSpan.FromMilliseconds(EbmlCodec.ToUInt64(raw));
                break;
            }
        }

        foreach (var child in cluster.Children)
        {
            if (child.Id == 0xA3) continue; // SimpleBlock

            if (child.Id == 0xA0 && child is MasterElement bg) // BlockGroup
            {
                byte[]? blockData = null;
                TimeSpan? duracao = null;

                foreach (var gc in bg.Children)
                {
                    if (gc.Id == 0xA1) // Block
                    {
                        if (gc.DataSize is < 4 or > 4096) continue;

                        var slice = gc.ElementToDataSlice();
                        int toRead = (int)Math.Min(peekBuffer.Length, slice.Length);
                        slice.ReadExactly(peekBuffer, 0, toRead);

                        var (trackNumber, _) = EbmlCodec.ReadVint(peekBuffer, 0);

                        if (trackNumbersFilter != null && !trackNumbersFilter.Contains(trackNumber))
                            continue;

                        blockData = gc.ElementDataToBytes();
                    }
                    else if (gc.Id == 0x9B) // BlockDuration
                    {
                        var raw = gc.ElementDataToBytes();
                        if (raw != null)
                            duracao = TimeSpan.FromMilliseconds(EbmlCodec.ToUInt64(raw));
                    }
                }

                if (blockData is null) continue;

                try
                {
                    var cue = ReadBlock(blockData, tracks, clusterTimestamp, duracao);
                    AddCue(cue, cuesPorTrack, maxCuesPerTrack);
                }
                catch { }
            }
        }
    }
    
    /// <summary>
    /// Decodifica um <c>Block</c> Matroska.
    /// </summary>
    /// <param name="data">Bytes do <c>Block</c> sem o header EBML.</param>
    /// <param name="tracks">Metadados das faixas conhecidas; usado para validar
    /// o TrackNumber lido.</param>
    /// <param name="clusterTimestamp">Timestamp absoluto do Cluster ao qual o
    /// Block pertence, usado como base para o timestamp do cue.</param>
    /// <param name="duracao">Duração opcional do cue, proveniente de
    /// <c>BlockDuration</c> (<c>0x9B</c>). Pode ser <c>null</c>.</param>
    /// <returns>
    /// Um <see cref="SubtitleCue"/> com TrackNumber, Timestamp absoluto,
    /// Duration e Data preenchidos; ou <c>null</c> se o bloco não representar um
    /// cue de legenda válido.
    /// </returns>
    private static SubtitleCue? ReadBlock(
    byte[] data,    
    Dictionary<ulong, SubtitleTrackMetadata> tracks,
    TimeSpan clusterTimestamp,
    TimeSpan? duracao)
    {
        if (data == null || data.Length < 4) return null;

        int offset = 0;

        var (trackNumber, tnLen) = EbmlCodec.ReadVint(data, offset);
        offset += tnLen;

        if (!tracks.ContainsKey(trackNumber)) return null;
        if (offset + 3 > data.Length) return null;

        short ts = (short)((data[offset] << 8) | data[offset + 1]);
        offset += 2;

        byte flags = data[offset];
        offset += 1;

        if ((flags & 0x06) != 0)
        {
            Log.Salvar($"[Subs] Lacing real no Track #{trackNumber} " +
                    $"({data.Length - offset} bytes descartados)");
            return null;
        }

        var payload = new byte[data.Length - offset];
        Buffer.BlockCopy(data, offset, payload, 0, payload.Length);

        return new SubtitleCue
        {
            TrackNumber = trackNumber,
            Timestamp   = clusterTimestamp + TimeSpan.FromMilliseconds(ts),
            Duration    = duracao,
            Data        = payload
        };
    }

    /// <summary>
    /// Adiciona um cue ao acumulador da faixa correspondente, respeitando o limite configurado.
    /// </summary>
    /// <param name="cue">Cue a adicionar. Ignorado se <c>null</c>.</param>
    /// <param name="cuesPorTrack">Acumulador de cues por <c>TrackNumber</c>.</param>
    /// <param name="maxPorTrack">Número máximo de cues por faixa.</param>
    private static void AddCue(
        SubtitleCue? cue,
        Dictionary<ulong, List<SubtitleCue>> cuesPorTrack,
        int maxPorTrack)
    {
        if (cue is null) return;

        if (!cuesPorTrack.TryGetValue(cue.TrackNumber, out var lista))
        {
            lista = new List<SubtitleCue>(Math.Min(maxPorTrack, 64));
            cuesPorTrack[cue.TrackNumber] = lista;
        }

        if (lista.Count < maxPorTrack)
            lista.Add(cue);
    }
}
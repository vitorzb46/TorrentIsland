using System.Text.Json;

namespace TorrentIsland.Infrastructure.Services;

public class JsonFiles
{
    private static readonly SemaphoreSlim _semaphore = new(1, 1);
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    /// <summary>
    /// Persiste de forma assíncrona o conteúdo de um dicionário em um arquivo JSON no disco.
    /// Esta operação é thread-safe, utilizando um semáforo para garantir que apenas uma gravação
    /// ocorra por vez, prevenindo corrupção de arquivo.
    /// </summary>
    /// <typeparam name="TKey">O tipo das chaves do dicionário.</typeparam>
    /// <typeparam name="TValue">O tipo dos valores do dicionário.</typeparam>
    /// <param name="instance">
    /// O dicionário (ex: <see cref="Dictionary{TKey, TValue}"/> ou <see cref="ConcurrentDictionary{TKey, TValue}"/>)
    /// contendo os dados a serem serializados e persistidos.
    /// </param>
    /// <param name="cachePath">
    /// O caminho físico completo (incluindo nome e extensão) do arquivo JSON de destino.
    /// </param>
    /// <returns>Uma tarefa que representa a operação assíncrona de escrita.</returns>
    public static async Task SaveToFileAsync<TKey, TValue>(IDictionary<TKey, TValue> instance,
                                                           string cachePath,
                                                           CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        await _semaphore.WaitAsync().ConfigureAwait(false);
        try
        {
            var json = JsonSerializer.Serialize(instance, Options);
            await File.WriteAllTextAsync(cachePath, json, ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw new OperationCanceledException();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Carrega de forma assíncrona os dados de um arquivo JSON e os insere no dicionário fornecido em memória.
    /// Esta operação é thread-safe e substitui completamente o conteúdo atual do dicionário pelos dados do arquivo.
    /// </summary>
    /// <typeparam name="TKey">O tipo das chaves do dicionário.</typeparam>
    /// <typeparam name="TValue">O tipo dos valores do dicionário.</typeparam>
    /// <param name="cachePath">
    /// O caminho físico completo (incluindo nome e extensão) do arquivo JSON de origem.
    /// </param>
    /// <param name="instance">
    /// A instância do dicionário em memória que receberá os dados carregados.
    /// O conteúdo existente será completamente limpo (<see cref="IDictionary{TKey, TValue}.Clear"/>) antes do carregamento.
    /// </param>
    /// <returns>Uma tarefa que representa a operação assíncrona de leitura.</returns>
    public static async Task LoadFromFileAsync<TKey, TValue>(IDictionary<TKey, TValue> instance,
                                                             string cachePath,
                                                             CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        await _semaphore.WaitAsync().ConfigureAwait(false);
        try
        {
            if (!File.Exists(cachePath)) return;
            var json = await File.ReadAllTextAsync(cachePath).ConfigureAwait(false);
            var data = JsonSerializer.Deserialize<IDictionary<TKey, TValue>>(json);
            if (data != null)
            {
                instance.Clear();
                foreach (var item in data)
                    instance[item.Key] = item.Value;
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"{ex.Message}");
        }
        finally
        {
            _semaphore.Release();
        }
    }
}
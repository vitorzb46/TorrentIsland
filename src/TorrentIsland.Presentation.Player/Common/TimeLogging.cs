using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace TorrentIsland.Presentation.Player.Common;

/// <summary>
/// Utilitário para medição de tempo de execução de operações síncronas e assíncronas.
/// Registra o tempo decorrido em milissegundos através do log personalizado <see cref="Log.Salvar(string)"/>.
/// </summary>
internal class TimeLogging
{
    /// <summary>
    /// Executa a operação síncrona <paramref name="action"/> e registra o tempo decorrido em milissegundos.
    /// </summary>
    /// <param name="action">
    /// A operação síncrona a ser executada e cronometrada. Não pode ser <see langword="null"/>.
    /// </param>
    /// <param name="caller">
    /// Nome do membro chamador, capturado automaticamente pelo compilador através de
    /// <see cref="CallerMemberNameAttribute"/>. Não deve ser informado explicitamente.
    /// </param>
    /// <param name="nameMethod">
    /// Nome opcional a ser exibido no log. Quando informado, tem precedência sobre o nome
    /// inferido do delegate e sobre <paramref name="caller"/>.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Lançada quando <paramref name="action"/> é <see langword="null"/>.
    /// </exception>
    public static Task Time(Action action, [CallerMemberName] string? caller = default, string? nameMethod = null)
    {
        ArgumentNullException.ThrowIfNull(action);

        var n = NameFallback(action, caller, nameMethod);

        return TimeCore(() =>
        {
            action();
            return Task.FromResult(true);
        }, n);
    }
    /// <summary>
    /// Executa <paramref name="func"/> de forma assíncrona e registra no log o tempo total decorrido em milissegundos.
    /// </summary>
    /// <param name="func">
    /// A operação assíncrona a ser executada e cronometrada. Não pode ser <see langword="null"/>.
    /// </param>
    /// <param name="caller">
    /// Nome do membro chamador, capturado automaticamente pelo compilador através de
    /// <see cref="CallerMemberNameAttribute"/>. Não deve ser informado explicitamente.
    /// </param>
    /// <param name="nameMethod">
    /// Nome opcional a ser exibido no log. Quando informado, tem precedência sobre o nome
    /// inferido do delegate e sobre <paramref name="caller"/>.
    /// </param>
    /// <returns>Uma <see cref="Task"/> que representa a conclusão da operação assíncrona.</returns>
    /// <exception cref="ArgumentNullException">
    /// Lançada quando <paramref name="func"/> é <see langword="null"/>.
    /// </exception>
    public static async Task Time(Func<Task> func, [CallerMemberName] string? caller = default, string? nameMethod = null)
    {
        ArgumentNullException.ThrowIfNull(func);

        var n = NameFallback(func, caller, nameMethod);

        await TimeCore(async () =>
        {
            await func().ConfigureAwait(false);
            return true;
        }, n).ConfigureAwait(false);
    }

    /// <summary>
    /// Executa <paramref name="func"/> de forma assíncrona, registra no log o tempo total decorrido
    /// em milissegundos.
    /// </summary>
    /// <typeparam name="T">Tipo do resultado retornado pela operação assíncrona.</typeparam>
    /// <param name="func">
    /// A operação assíncrona a ser executada, cronometrada e cujo resultado será retornado.
    /// Não pode ser <see langword="null"/>.
    /// </param>
    /// <param name="caller">
    /// Nome do membro chamador, capturado automaticamente pelo compilador através de
    /// <see cref="CallerMemberNameAttribute"/>. Não deve ser informado explicitamente.
    /// </param>
    /// <param name="nameMethod">
    /// Nome opcional a ser exibido no log. Quando informado, tem precedência sobre o nome
    /// inferido do delegate e sobre <paramref name="caller"/>.
    /// </param>
    /// <returns>O valor produzido por <paramref name="func"/>.</returns>
    /// <exception cref="ArgumentNullException">
    /// Lançada quando <paramref name="func"/> é <see langword="null"/>.
    /// </exception>
    public static async Task<T> Time<T>(Func<Task<T>> func, [CallerMemberName] string? caller = default, string? nameMethod = null)
    {
        ArgumentNullException.ThrowIfNull(func);

        var n = NameFallback(func, caller, nameMethod);

        return await TimeCore(func, n).ConfigureAwait(false);
    }

    /// <summary>
    /// <see cref="Func{TResult}"/> genérico de cronometragem: executa <paramref name="func"/>, mede o tempo decorrido
    /// e registra o resultado no log, independentemente de sucesso ou exceção.
    /// </summary>
    /// <typeparam name="T">Tipo do resultado retornado pela operação assíncrona.</typeparam>
    /// <param name="func">A operação assíncrona a ser executada e cronometrada.</param>
    /// <param name="fallback">
    /// Nome já resolvido a ser exibido no log. A resolução ocorre nas sobrecargas públicas
    /// de <see cref="Time(Func{Task}, string?, string?)"/> e
    /// <see cref="Time{T}(Func{Task{T}}, string?, string?)"/>, antes de qualquer adaptação
    /// de delegate, para preservar o nome original do método.
    /// </param>
    /// <returns>O resultado produzido por <paramref name="func"/>.</returns>
    private static async Task<T> TimeCore<T>(Func<Task<T>> func, string fallback)
    {
        T result;

        var sw = Stopwatch.StartNew();
        try
        {
            result = await func().ConfigureAwait(false);
        }
        finally
        {
            sw.Stop();
            Log.Salvar($"[Tempo] {fallback}: {sw.ElapsedMilliseconds} ms");
        }

        return result;
    }

    /// <summary>
    /// Resolve o nome a ser exibido no log, aplicando a seguinte ordem de prioridade:
    /// <list type="number">
    ///   <item><description><paramref name="nameMethod"/>, quando informado explicitamente.</description></item>
    ///   <item><description>O nome legível extraído do delegate, via <see cref="DelegateName(Delegate)"/>.</description></item>
    ///   <item><description>O nome do membro chamador capturado por <see cref="CallerMemberNameAttribute"/>.</description></item>
    ///   <item><description>A string literal <c>"Desconhecido"</c>, como último recurso.</description></item>
    /// </list>
    /// </summary>
    /// <param name="d">O delegate cujo nome será tentado como fonte secundária.</param>
    /// <param name="caller">Nome do membro chamador, fornecido pelo compilador.</param>
    /// <param name="nameMethod">Nome explícito opcional.</param>
    /// <returns>Uma string não nula representando o nome a ser exibido no log.</returns>
    private static string NameFallback(Delegate d, string? caller, string? nameMethod)
        => nameMethod ?? DelegateName(d) ?? caller ?? "Desconhecido";

    /// <summary>
    /// Tenta extrair o nome legível do método encapsulado por um <see cref="Delegate"/>.
    /// </summary>
    /// <param name="d">O delegate do qual se deseja obter o nome do método.</param>
    /// <returns>
    /// O nome do método encapsulado, ou <see langword="null"/> caso o delegate referencie
    /// um lambda ou método anônimo.
    /// </returns>
    private static string? DelegateName(Delegate d)
    {
        var name = d.Method.Name;
        return name.Contains('<') ? null : name;
    }
}

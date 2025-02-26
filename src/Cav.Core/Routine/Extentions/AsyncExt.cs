namespace Cav;

#pragma warning disable CA1062 // Проверить аргументы или открытые методы

/// <summary>
/// Расширения для вызова типовых расширений LINQ для асинхронных объектов
/// </summary>
public static class AsyncExt
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TSource"></typeparam>
    /// <param name="source"></param>
    /// <returns></returns>
    public static async Task<TSource?> FirstOrDefaultAsync<TSource>(this Task<IEnumerable<TSource>> source) => (await source.ConfigureAwait(false)).FirstOrDefault();
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TSource"></typeparam>
    /// <param name="source"></param>
    /// <param name="predicate"></param>
    /// <returns></returns>
    public static async Task<TSource?> FirstOrDefaultAsync<TSource>(this Task<IEnumerable<TSource>> source, Func<TSource, bool> predicate) =>
        (await source.ConfigureAwait(false)).FirstOrDefault(predicate);
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TSource"></typeparam>
    /// <param name="source"></param>
    /// <returns></returns>
    public static async Task<TSource?> SingleOrDefaultAsync<TSource>(this Task<IEnumerable<TSource>> source) => (await source.ConfigureAwait(false)).SingleOrDefault();
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TSource"></typeparam>
    /// <param name="source"></param>
    /// <param name="predicate"></param>
    /// <returns></returns>
    public static async Task<TSource?> SingleOrDefaultAsync<TSource>(this Task<IEnumerable<TSource>> source, Func<TSource, bool> predicate) =>
        (await source.ConfigureAwait(false)).SingleOrDefault(predicate);
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="source"></param>
    /// <returns></returns>
    public static async Task<bool> AnyAsync<T>(this Task<IEnumerable<T>> source) => (await source.ConfigureAwait(false)).Any();
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TSource"></typeparam>
    /// <param name="source"></param>
    /// <param name="predicate"></param>
    /// <returns></returns>
    public static async Task<bool> AnyAsync<TSource>(this Task<IEnumerable<TSource>> source, Func<TSource, bool> predicate) => (await source.ConfigureAwait(false)).Any(predicate);
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TSource"></typeparam>
    /// <typeparam name="TResult"></typeparam>
    /// <param name="source"></param>
    /// <param name="selector"></param>
    /// <returns></returns>
    public static async Task<IEnumerable<TResult>> SelectAsync<TSource, TResult>(
        this Task<IEnumerable<TSource>> source, Func<TSource, TResult> selector) => (await source.ConfigureAwait(false)).Select(selector);
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TSource"></typeparam>
    /// <typeparam name="TResult"></typeparam>
    /// <param name="source"></param>
    /// <param name="selector"></param>
    /// <returns></returns>
    public static async Task<IEnumerable<TResult>> SelectAsync<TSource, TResult>(
        this Task<IEnumerable<TSource>> source, Func<TSource, Task<TResult>> selector)
    {
        var res = new List<TResult>();
        foreach (var item in await source.ConfigureAwait(false))
            res.Add(await selector(item).ConfigureAwait(false));
        return res;
    }
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TSource"></typeparam>
    /// <typeparam name="TResult"></typeparam>
    /// <param name="source"></param>
    /// <param name="selector"></param>
    /// <returns></returns>
    public static async Task<IEnumerable<TResult>> SelectManyAsync<TSource, TResult>(this Task<IEnumerable<TSource>> source, Func<TSource, IEnumerable<TResult>> selector) =>
        (await source.ConfigureAwait(false)).SelectMany(selector);
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="source"></param>
    /// <returns></returns>
    public static async Task<List<T>> ToListAsync<T>(
        this Task<IEnumerable<T>> source) => (await source.ConfigureAwait(false)).ToList();
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TSource"></typeparam>
    /// <param name="source"></param>
    /// <param name="predicate"></param>
    /// <returns></returns>
    public static async Task<IEnumerable<TSource>> WhereAsync<TSource>(this Task<IEnumerable<TSource>> source, Func<TSource, bool> predicate) =>
        (await source.ConfigureAwait(false)).Where(predicate);
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="task"></param>
    /// <returns></returns>
    public static async Task<T?> JsonDeserealizeAsync<T>(this Task<string?> task) => (await task.ConfigureAwait(false)).JsonDeserealize<T>();
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="task"></param>
    /// <returns></returns>
    public static async Task<string?> JsonSerializeAsync<T>(this Task<T> task) => (await task.ConfigureAwait(false)).JsonSerialize();
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="sourceTask"></param>
    /// <param name="actionForEach"></param>
    /// <returns></returns>
    public static async Task ForEachAsync<T>(this Task<IEnumerable<T>> sourceTask, Func<T, Task> actionForEach)
    {
        foreach (var item in await sourceTask.ConfigureAwait(false))
            await actionForEach(item).ConfigureAwait(false);
    }
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="source"></param>
    /// <param name="actionForEach"></param>
    /// <returns></returns>
    public static async Task ForEachAsync<T>(this IEnumerable<T> source, Func<T, Task> actionForEach)
    {
        foreach (var item in source)
            await actionForEach(item).ConfigureAwait(false);
    }
}

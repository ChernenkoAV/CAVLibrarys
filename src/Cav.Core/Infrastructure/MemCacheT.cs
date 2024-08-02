using System.Runtime.Caching;

namespace Cav.Infrastructure;

/// <summary>
/// Конечная реализация кэша в памяти для <see cref="MemoryCache"/> с минимальным функционалом.
/// Время "жизни" объекта - 5 минут. Остальные настроки кэша - по умолчанию.
/// Не думаю, что стоит использовать этот класс в конструкторе.
/// </summary>
/// <typeparam name="TValue"></typeparam>
public sealed class MemCache<TValue>
    where TValue : class
{
    private Func<string, TValue?>? getMissingValue;
    private Func<string, Task<TValue?>>? getMissingValueAsync;
    private static MemoryCache memCache = new(nameof(MemoryCache));

    private static Dictionary<string, SemaphoreSlim> semaphore = [];

    private string innerMarker = Guid.NewGuid().ToShortString();

    public MemCache() =>
        semaphore[innerMarker] = new(1, 1);

    public MemCache(Func<string, TValue?> missingGetter) : this() =>
        getMissingValue = missingGetter ?? throw new ArgumentNullException(nameof(missingGetter));

    public MemCache(Func<string, Task<TValue?>> missingGetterAsync) : this() =>
        getMissingValueAsync = missingGetterAsync ?? throw new ArgumentNullException(nameof(missingGetterAsync));

    public void SetMissingGetter(Func<string, TValue?> missingGetter) =>
        getMissingValue = missingGetter ?? throw new ArgumentNullException(nameof(missingGetter));

    public void SetMissingGetter(Func<string, Task<TValue?>> missingGetterAsync) =>
        missingGetterAsync = missingGetterAsync ?? throw new ArgumentNullException(nameof(missingGetterAsync));

    private string createKey(string key) => $"{innerMarker}|{key}";
    public TValue? Get(string key, bool escalateException = false) =>
        GetAsync(key, escalateException).GetAwaiter().GetResult();

    public async Task<TValue?> GetAsync(string key, bool escalateException = false)
    {
        if (key.IsNullOrWhiteSpace())
            return default;

        await semaphore[innerMarker].WaitAsync().ConfigureAwait(false);

        try
        {
            var res = memCache.Get(createKey(key!)) as TValue;
            if (res is null)
            {
                if (getMissingValueAsync is not null)
                    res = await getMissingValueAsync(key!).ConfigureAwait(false);

                if (getMissingValue is not null)
                    res = getMissingValue(key!);

                if (res is not null)
                    Add(key, res);
            }

            return res;
        }
        catch
        {
            if (escalateException)
                throw;
            else
                return default;
        }
        finally
        {
            semaphore[innerMarker].Release();
        }
    }

    /// <summary>
    /// Элементы <see langword="null"/> не добавляются в кэш
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <exception cref="ArgumentException"></exception>
    public TValue? Add(string key, TValue? value)
    {
        if (value is null)
            return value;

        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException($"\"{nameof(key)}\" не может быть пустым или содержать только пробел.", nameof(key));

        key = createKey(key);

        memCache.Set(key, value, DateTimeOffset.UtcNow.AddMinutes(5));

        return value;
    }
}


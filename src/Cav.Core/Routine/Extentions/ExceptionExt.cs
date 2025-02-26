using System.Collections;
using System.Reflection;
using System.Text;

namespace Cav;

/// <summary>
/// Расширения для исключений
/// </summary>
public static class ExceptionExt
{
    private const string padder = "-----";
    /// <summary>
    /// Развертывание текста исключения
    /// </summary>
    /// <param name="ex"></param>
    /// <param name="refinedDecoding">приоритетная дополнительная логика раскодирования исключения</param>
    /// <returns></returns>
    public static string Expand(this Exception ex, Func<Exception, string>? refinedDecoding = null)
    {
        var builder = new StringBuilder();
        if (ex == null)
            return string.Empty;

        builder.AppendLine(ex.Message);

        if (ex.Data.Count > 0)
        {
#pragma warning disable IDE0220 // Добавить явное приведение
            foreach (DictionaryEntry de in ex.Data)
                builder.AppendLine($"{de.Key}: {de.Value}");
#pragma warning restore IDE0220 // Добавить явное приведение
        }

        if (refinedDecoding != null)
            builder.AppendLine(refinedDecoding(ex));

        builder.AppendLine($"Type: {ex.GetType().FullName}");

        if (ex.TargetSite != null)
            builder.AppendLine($"TargetSite: {ex.TargetSite}");

        if (!ex.StackTrace.IsNullOrWhiteSpace())
            builder.AppendLine($"StackTrace->{ex.StackTrace}");

        if (ex is ReflectionTypeLoadException reflectEx && reflectEx.LoaderExceptions != null)
        {
            builder.AppendLine(padder);
            builder.AppendLine("LoaderExceptions:");
            foreach (var rEx in reflectEx.LoaderExceptions.Where(x => x != null))
                builder.AppendLine(rEx!.Expand(refinedDecoding));
        }

        if (ex.InnerException != null)
        {
            builder.AppendLine(padder);
            builder.AppendLine("InnerException:");
            builder.AppendLine(ex.InnerException.Expand(refinedDecoding));
        }

        if (ex is AggregateException agrEx && agrEx.InnerExceptions != null)
            foreach (var inEx in agrEx.InnerExceptions)
            {
                builder.AppendLine(padder);
                builder.AppendLine("InnerException:");
                builder.AppendLine(inEx.Expand(refinedDecoding));
            }

        return builder.ToString();
    }
}

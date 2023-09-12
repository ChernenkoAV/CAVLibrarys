using System.Reflection;

namespace Cav;

/// <summary>
/// Расширения для исключений
/// </summary>
#pragma warning disable CA1711 // Идентификаторы не должны иметь неправильных суффиксов
public static class ExtException
#pragma warning restore CA1711 // Идентификаторы не должны иметь неправильных суффиксов
{
    /// <summary>
    /// Развертывание текста исключения
    /// </summary>
    /// <param name="ex"></param>
    /// <param name="refinedDecoding">приоритетная дополнительная логика раскодирования исключения</param>
    /// <returns></returns>
    public static string Expand(this Exception ex, Func<Exception, string>? refinedDecoding = null)
    {
        string res = null!;
        if (ex == null)
            return res;

        res = $"Message: {ex.Message}{Environment.NewLine}";

        if (refinedDecoding != null)
            res = refinedDecoding(ex);

        res += $"Type: {ex.GetType().FullName}{Environment.NewLine}";

        if (ex.TargetSite != null)
            res += $"TargetSite: {ex.TargetSite}{Environment.NewLine}";

        if (!ex.StackTrace.IsNullOrWhiteSpace())
            res += $"StackTrace->{ex.StackTrace}{Environment.NewLine}";

        if (ex.GetType().BaseType!.Name == "FaultException" &&
            ex.GetPropertyValueNestedObject("Detail")?.GetType().Name == "ExceptionDetail")
        {
            res += Environment.NewLine.PadLeft(5, '*');

            var exdetail = ex.GetPropertyValueNestedObject("Detail");

            while (exdetail != null)
            {
                res += $"Detail Type: {exdetail.GetPropertyValueNestedObject("Type")}{Environment.NewLine}";
                res += $"Detail Message: {exdetail.GetPropertyValueNestedObject("Message")}{Environment.NewLine}";
                if (!(exdetail.GetPropertyValueNestedObject("StackTrace") as string).IsNullOrWhiteSpace())
                    res += $"Detail StackTrace->{Environment.NewLine}{exdetail.GetPropertyValueNestedObject("StackTrace")}{Environment.NewLine}";
                if (exdetail.GetPropertyValueNestedObject("InnerException") != null)
                    res += $"***{Environment.NewLine}Detail InnerException->{Environment.NewLine}";
                exdetail = exdetail.GetPropertyValueNestedObject("InnerException");
            }
        }

        if (ex is ReflectionTypeLoadException reflectEx && reflectEx.LoaderExceptions != null)
        {
            res += $"{Environment.NewLine.PadLeft(20, '-')}LoaderExceptions ->";
            foreach (var rEx in reflectEx.LoaderExceptions.Where(x => x != null))
                res += Environment.NewLine + rEx!.Expand(refinedDecoding);
        }

        if (ex.InnerException != null)
            res += $"{Environment.NewLine.PadLeft(20, '-')}InnerException->{Environment.NewLine}{ex.InnerException.Expand(refinedDecoding)}";

        if (ex is AggregateException agrEx && agrEx.InnerExceptions != null)
            foreach (var inEx in agrEx.InnerExceptions)
                res += $"{Environment.NewLine.PadLeft(20, '-')}InnerException->{Environment.NewLine}{inEx.Expand(refinedDecoding)}";

        return res;
    }
}

using System;
using System.Linq;
using System.Reflection;

namespace Cav
{
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
        public static String Expand(this Exception ex, Func<Exception, string> refinedDecoding = null)
        {
            String res = null;
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

            if (ex is ReflectionTypeLoadException reflectEx && reflectEx.LoaderExceptions != null)
            {
                res += $"{Environment.NewLine.PadLeft(20, '-')}LoaderExceptions ->";
                foreach (var rEx in reflectEx.LoaderExceptions.Where(x => x != null))
                    res += Environment.NewLine + rEx.Expand(refinedDecoding);
            }

            if (ex.InnerException != null)
                res += $"{Environment.NewLine.PadLeft(20, '-')}InnerException->{Environment.NewLine}{ex.InnerException.Expand(refinedDecoding)}";

            if (ex is AggregateException agrEx && agrEx.InnerExceptions != null)
                foreach (var inEx in agrEx.InnerExceptions)
                    res += $"{Environment.NewLine.PadLeft(20, '-')}InnerException->{Environment.NewLine}{inEx.Expand(refinedDecoding)}";

            return res;
        }
    }
}

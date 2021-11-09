using System;
using System.Linq;
using System.Reflection;
using System.ServiceModel;

namespace Cav
{
    /// <summary>
    /// Расширения для исключений
    /// </summary>
    public static class ExtException
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
                res += $"TargetSite: {ex.TargetSite.ToString()}{Environment.NewLine}";

            if (!ex.StackTrace.IsNullOrWhiteSpace())
                res += $"StackTrace->{ex.StackTrace}{Environment.NewLine}";

            var commEx = ex as FaultException;
            if (commEx != null)
            {
                ExceptionDetail exdetail = commEx.GetPropertyValueNestedObject("Detail") as ExceptionDetail;

                if (exdetail != null)
                    res += Environment.NewLine.PadLeft(5, '*');

                while (exdetail != null)
                {
                    res += $"Detail Type: {exdetail.Type}{Environment.NewLine}";
                    res += $"Detail Message: {exdetail.Message}{Environment.NewLine}";
                    if (!exdetail.StackTrace.IsNullOrWhiteSpace())
                        res += $"Detail StackTrace->{Environment.NewLine}{exdetail.StackTrace}{Environment.NewLine}";
                    if (exdetail.InnerException != null)
                        res += $"***{Environment.NewLine}Detail InnerException->{Environment.NewLine}";
                    exdetail = exdetail.InnerException;
                }
            }

            var reflectEx = ex as ReflectionTypeLoadException;
            if (reflectEx != null && reflectEx.LoaderExceptions != null)
            {
                res += $"{Environment.NewLine.PadLeft(20, '-')}LoaderExceptions ->";
                foreach (var rEx in reflectEx.LoaderExceptions.Where(x => x != null))
                    res += Environment.NewLine + rEx.Expand(refinedDecoding);
            }

            if (ex.InnerException != null)
                res += $"{Environment.NewLine.PadLeft(20, '-')}InnerException->{Environment.NewLine}{ex.InnerException.Expand(refinedDecoding)}";

            var agrEx = ex as AggregateException;
            if (agrEx != null && agrEx.InnerExceptions != null)
                foreach (var inEx in agrEx.InnerExceptions)
                    res += $"{Environment.NewLine.PadLeft(20, '-')}InnerException->{Environment.NewLine}{inEx.Expand(refinedDecoding)}";

            return res;
        }
    }
}

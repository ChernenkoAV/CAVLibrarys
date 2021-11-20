using System;
using System.ServiceModel;

namespace Cav
{
    /// <summary>
    /// Расширение для исключений
    /// </summary>
#pragma warning disable CA1711 // Идентификаторы не должны иметь неправильных суффиксов
    public static class ExtException
#pragma warning restore CA1711 // Идентификаторы не должны иметь неправильных суффиксов
    {
        /// <summary>
        /// Развертывание ошибок Wcf
        /// </summary>
        /// <param name="ex"></param>
        /// <returns></returns>
        public static string Expand(this FaultException ex)
        {
            string res = null;

            var exdetail = ex.GetPropertyValueNestedObject("Detail") as ExceptionDetail;

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

            return res;
        }
    }
}

using System;
using System.Data.SqlClient;

namespace Cav
{
    /// <summary>
    /// Расширения для исключений
    /// </summary>
    public static class ExtException
    {
        /// <summary>
        /// Развертывание текста исключения + обработка SqlException
        /// </summary>
        /// <param name="ex"></param>
        /// <returns></returns>
        public static String Expand(this Exception ex)
        {
            return
                ex == null
                ? String.Empty
                : (ex is SqlException
                    ? "SqlException №:" + ((SqlException)ex).Number + " " + ex.Message
                    : "Message: " + ex.Message + (ex.StackTrace.IsNullOrWhiteSpace() ? String.Empty : Environment.NewLine + "StackTrace: " + ex.StackTrace)) +
                  Environment.NewLine +
                  ex.InnerException.Expand();
        }
    }
}

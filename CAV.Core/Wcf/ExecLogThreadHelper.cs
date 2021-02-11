using System;
using System.Threading.Tasks;
using Cav.Soap;

namespace Cav.Wcf
{
    /// <summary>
    /// Хелпер для запуска метода логирования в потоке
    /// </summary>
    internal static class ExecLogThreadHelper
    {
        public static void WriteLog(ISoapPackageLog logger, SoapPackage p)
        {
            if (logger == null)
                return;

            Task.Factory.StartNew(o =>
            {
                try
                {
                    var par = (Tuple<Action<SoapPackage>, SoapPackage>)o;
                    par.Item1(par.Item2);
                }
                catch
                { }
            },
            Tuple.Create<Action<SoapPackage>, SoapPackage>(logger.ActionLog, p));
        }

        public static void WriteLog(Action<MessageLogData> logger, MessageLogData p)
        {
            Task.Factory.StartNew(o =>
            {
                try
                {
                    var par = (Tuple<Action<MessageLogData>, MessageLogData>)o;
                    par.Item1(par.Item2);
                }
                catch
                { }
            },
            Tuple.Create<Action<MessageLogData>, MessageLogData>(logger, p));
        }

        public static void WriteLog(Action<Exception> logger, Exception p)
        {
            Task.Factory.StartNew(o =>
            {
                try
                {
                    var par = (Tuple<Action<Exception>, Exception>)o;
                    par.Item1(par.Item2);
                }
                catch
                { }
            },
            Tuple.Create<Action<Exception>, Exception>(logger, p));
        }
    }
}

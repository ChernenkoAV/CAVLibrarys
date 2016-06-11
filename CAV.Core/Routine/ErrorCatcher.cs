using System;
using System.Diagnostics;
using System.IO;

namespace Cav
{
    /// <summary>
    /// Отображение исключений с занесением в лог. Криво рабтает в службах(не юзать).
    /// </summary>
    [Obsolete("Не доделано. Не юзать", true)]
    public static partial class ErrorCatcher
    {
        /// <summary>
        /// Путь с файлом лога.
        /// </summary>
        private static String logfile = Path.Combine(DomainContext.AppDataUserStorage, "trace " + DateTime.Now.ToString("yyyy-MM-dd") + ".log");

        /// <summary>
        /// Экземпляр лога собылтий Windows. Он же флаг, что подсистема логирования инициализированна
        /// </summary>
        private static EventLog eventLog = null;

        /// <summary>Инициализация с внешним источником</summary>
        /// <remarks>Черненко А.В.,.</remarks>
        /// <param name="Event">The event.</param>
        public static void Init(EventLog Event)
        {
            eventLog = Event;
        }

        ///// <summary>
        ///// Инициализация. Отдельный метод для приложений
        ///// </summary>
        //public static void Init()
        //{
        //    Init(DomainContext.NameEntryAssembly, "РНТ");
        //}

        ///// <summary>
        ///// Инициализация
        ///// </summary>
        //public static void Init(String Sourse, String NameLog)
        //{
        //    if (eventLog != null)
        //        return;

        //    if (Sourse.IsNullOrWhiteSpace())
        //        throw new Exception("Пустое значение в имени источника для журнала windows");

        //    Debug.Listeners.Add(new ConsoleTraceListener());
        //    Debug.AutoFlush = true;

        //    Trace.Listeners.Add(new TextWriterTraceListener(logfile));
        //    Trace.AutoFlush = true;

        //    eventLog = new EventLog();
        //    eventLog.Source = Sourse;

        //    try
        //    {
        //        eventLog.WriteEntry("Приложение запущенно", EventLogEntryType.Information);
        //    }
        //    catch
        //    {
        //        try
        //        {
        //            string fileForExec = DomainContext.TempPath + DomainContext.NameEntryAssembly + ".exe";
        //            using (FileStream fs = File.Create(fileForExec))
        //                fs.Write(Properties.Resources.CreateEventSource, 0, Properties.Resources.CreateEventSource.Length);

        //            Process.Start(fileForExec, Sourse + " " + NameLog).WaitForExit();
        //            File.Delete(fileForExec);
        //        }
        //        catch (Exception ex)
        //        {
        //            // TODO Переделать на измемнение источника... Если не удалось создать свой, то  заюзать журнал "Приложения"
        //            try
        //            {
        //                using (TextWriter tw = File.CreateText(logfile))
        //                {
        //                    tw.WriteLine();
        //                    tw.Write(DateTime.Now.ToLongDateString());
        //                    tw.WriteLine(" Создание источника для журнала событий утилитой неуспешно.");
        //                    tw.WriteLine("Текст:{0}", ex.Expand());
        //                    tw.WriteLine("Стек:{0}", ex.StackTrace);
        //                }
        //                System.Windows.Forms.MessageBox.Show("Ошибка в файле " + logfile, "Ошибка", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
        //            }
        //            catch (Exception ex2)
        //            {
        //                System.Windows.Forms.MessageBox.Show(ex2.Expand(), "Глобальная ошибка. Продолжение не возможно.", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
        //                Environment.Exit(11);
        //            }
        //        }

        //        eventLog = null;
        //    }
        //}

        /// <summary>Сохранение в журнал исключения</summary>
        /// <param name="ex">Исключение</param>
        public static void SaveToLogWithoutDialog(Exception ex)
        {
            if (eventLog == null)
                return;

            String msg = ex.Expand();
            msg = (ex.TargetSite == null ? String.Empty : "TargetSite: " + ex.TargetSite.ToString() + Environment.NewLine) + msg;
            
            if (msg.Length > 30000)
            {
                Trace.Write(msg);
                msg = String.Format("Полный текст в file:///{0}" + Environment.NewLine + "{1}", logfile, msg.Substring(0, 300));
                eventLog.WriteEntry(msg, EventLogEntryType.Error);
                return;
            }

            Debug.Write(msg);
            eventLog.WriteEntry(msg, EventLogEntryType.Error);
        }
    }
}

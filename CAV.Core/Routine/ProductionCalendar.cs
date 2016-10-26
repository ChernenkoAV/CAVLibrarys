using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Xml.Serialization;

namespace Cav.Routine
{
    /// <summary>
    /// Получение производственного календаря
    /// </summary>
    [XmlRoot("calendar")]
    public class ProductionCalendar
    {
        public ProductionCalendar()
        {
            Holidays = new List<HoliDay>();
        }
        public class HoliDay
        {
            [XmlAttribute("id")]
            public int ID { get; set; }
            [XmlAttribute("title")]
            public String Title { get; set; }
        }

        public class Day
        {
            [XmlAttribute("d")]
            public String DayMonth { get; set; }
            [XmlAttribute("t")]
            public TypeHoliDay Kind { get; set; }
            [XmlAttribute("h")]
            public int HoliID { get; set; }

            [XmlIgnore]
            public DateTime Date { get; set; }
        }

        public enum TypeHoliDay
        {
            [XmlEnum("0")]
            Undifane = 0,
            [XmlEnum("1")]
            Fiesta = 1,
            [XmlEnum("2")]
            ShortDay = 2,
            [XmlEnum("3")]
            WorkDay = 3
        }

        [XmlAttribute("year")]
        public int Year { get; set; }

        [XmlArray("holidays")]
        [XmlArrayItem("holiday")]
        public List<HoliDay> Holidays { get; set; }
        [XmlArray("days")]
        [XmlArrayItem("day")]
        public List<Day> Days { get; set; }

        public enum HolidayKind
        {
            Fiesta,
            Weekend
        }

        public struct Holiday
        {
            public DateTime Date { get; set; }
            public HolidayKind Kind { get; set; }
            public String Note { get; set; }
        }

        /// <summary>
        /// Получение всех нерабочих дней за указанный год. Данные берутся с сайта xmlcalendar.ru. Календарь без региональных праздников, без коротких дней.
        /// </summary>
        /// <param name="year">Год, за который необходимо получить данные</param>
        /// <returns>Нерабочие дни </returns>
        public static List<Holiday> GetAllHolidays(int year)
        {
            String url = "http://xmlcalendar.ru/data/ru/{0}/calendar.xml";
            url = String.Format(url, year);

            String bodyXML = null;
            List<Holiday> res = new List<Holiday>();

            var wreq = WebRequest.Create(url);
            using (var wresp = wreq.GetResponse())
            using (var sr = new StreamReader(wresp.GetResponseStream()))
                bodyXML = sr.ReadToEnd();

            var cdr = bodyXML.XMLDeserialize<ProductionCalendar>();

            foreach (var day in cdr.Days)
                day.Date = DateTime.Parse(day.DayMonth + "." + cdr.Year.ToString(), CultureInfo.InvariantCulture);

            DateTime date = new DateTime(year, 1, 1).AddDays(-1);
            DateTime dateend = new DateTime(year + 1, 1, 1);

            while (date < dateend)
            {
                date = date.AddDays(1);

                var day = cdr.Days.FirstOrDefault(x => x.Date == date);
                if (day != null)
                {
                    if (day.Kind == TypeHoliDay.ShortDay || day.Kind == TypeHoliDay.WorkDay)
                        continue;

                    var h = new Holiday();
                    h.Date = date;
                    h.Kind = HolidayKind.Fiesta;

                    var hnote = cdr.Holidays.FirstOrDefault(x => x.ID == day.HoliID);
                    if (hnote != null)
                        h.Note = hnote.Title;
                    else
                        h.Note = "Праздник";

                    res.Add(h);

                    continue;
                }

                if (date.DayOfWeek == DayOfWeek.Sunday || date.DayOfWeek == DayOfWeek.Saturday)
                {
                    var h = new Holiday();
                    h.Date = date;
                    h.Kind = HolidayKind.Weekend;
                    h.Note = "Выходной";
                    res.Add(h);
                }
            }

            return res;
        }
    }
}

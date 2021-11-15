﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Xml.Serialization;

namespace Cav.Routine
{
    /// <summary>
    /// Получение производственного календаря. Данные берутся из http://xmlcalendar.ru
    /// </summary>
    [XmlRoot("calendar")]
    public class ProductionCalendar
    {
        /// <summary>
        /// для сериализации
        /// </summary>
#pragma warning disable CA1034 // Вложенные типы не должны быть видимыми
        public class HoliDayDto
        {
            /// <summary>
            /// для сериализации
            /// </summary>
            [XmlAttribute("id")]
            public int ID { get; set; }
            /// <summary>
            /// для сериализации
            /// </summary>
            [XmlAttribute("title")]
            public String Title { get; set; }
        }
        /// <summary>
        /// для сериализации
        /// </summary>
        public class DayDto
        {
            /// <summary>
            /// для сериализации
            /// </summary>
            [XmlAttribute("d")]
            public String DayMonth { get; set; }
            /// <summary>
            /// для сериализации
            /// </summary>
            [XmlAttribute("t")]
            public TypeHoliDay Kind { get; set; }
            /// <summary>
            /// для сериализации
            /// </summary>
            [XmlAttribute("h")]
            public int HoliID { get; set; }
            /// <summary>
            /// для сериализации
            /// </summary>
            [XmlIgnore]
            public DateTime Date { get; set; }
        }

        /// <summary>
        /// для сериализации
        /// </summary>
        public enum TypeHoliDay
        {
            /// <summary>
            /// для сериализации
            /// </summary>
            [XmlEnum("0")]
            Undifane = 0,
            /// <summary>
            /// для сериализации
            /// </summary>
            [XmlEnum("1")]
            Fiesta = 1,
            /// <summary>
            /// для сериализации
            /// </summary>
            [XmlEnum("2")]
            ShortDay = 2,
            /// <summary>
            /// для сериализации
            /// </summary>
            [XmlEnum("3")]
            WorkDay = 3
        }
        /// <summary>
        /// для сериализации
        /// </summary>
        [XmlAttribute("year")]
        public int Year { get; set; }
        /// <summary>
        /// для сериализации
        /// </summary>
        [XmlArray("holidays")]
        [XmlArrayItem("holiday")]
        public List<HoliDayDto> Holidays { get; set; } = new List<HoliDayDto>();
        /// <summary>
        /// для сериализации
        /// </summary>
        [XmlArray("days")]
        [XmlArrayItem("day")]
        public List<DayDto> Days { get; set; } = new List<DayDto>();

        /// <summary>
        /// Тип выходного(нерабочего дня)
        /// </summary>
        public enum HolidayKind
        {
            /// <summary>
            /// Праздник
            /// </summary>
            Fiesta,
            /// <summary>
            /// Конец недели (суббота воскресение)
            /// </summary>
            Weekend
        }

        /// <summary>
        /// Нерабочий день
        /// </summary>
#pragma warning disable CA1815 // Переопределите операторы Equals и равенства для типов значений
        public struct Holiday
#pragma warning restore CA1815 // Переопределите операторы Equals и равенства для типов значений
        {
            /// <summary>
            /// Дата
            /// </summary>
            public DateTime Date { get; set; }
            /// <summary>
            /// Тип
            /// </summary>
            public HolidayKind Kind { get; set; }
            /// <summary>
            /// Описание
            /// </summary>
            public String Note { get; set; }
        }

        /// <summary>
        /// Получение всех нерабочих дней за указанный год. Данные берутся с сайта xmlcalendar.ru. Календарь без региональных праздников, без коротких дней.
        /// </summary>
        /// <param name="year">Год, за который необходимо получить данные</param>
        /// <returns>Нерабочие дни </returns>
        public static List<Holiday> GetAllHolidays(int year)
        {
            var url = "http://xmlcalendar.ru/data/ru/{0}/calendar.xml";
            url = String.Format(url, year);

            String bodyXML = null;
            var res = new List<Holiday>();

            var wreq = WebRequest.Create(new Uri(url));
            using (var wresp = wreq.GetResponse())
            using (var sr = new StreamReader(wresp.GetResponseStream()))
                bodyXML = sr.ReadToEnd();

            var cdr = bodyXML.XMLDeserialize<ProductionCalendar>();

            if (!cdr.Days.Any())
                return res;

            foreach (var day in cdr.Days)
                day.Date = DateTime.Parse(day.DayMonth + "." + cdr.Year.ToString(), CultureInfo.InvariantCulture);

            var date = new DateTime(year, 1, 1).AddDays(-1);
            var dateend = new DateTime(year + 1, 1, 1);

            while (date < dateend)
            {
                date = date.AddDays(1);

                var day = cdr.Days.FirstOrDefault(x => x.Date == date);
                if (day != null)
                {
#pragma warning disable IDE0078 // Используйте сопоставление шаблонов
                    if (day.Kind == TypeHoliDay.ShortDay || day.Kind == TypeHoliDay.WorkDay)
#pragma warning restore IDE0078 // Используйте сопоставление шаблонов
                        continue;

                    var h = new Holiday();
                    h.Date = date;
                    h.Kind = HolidayKind.Fiesta;

                    var hnote = cdr.Holidays.FirstOrDefault(x => x.ID == day.HoliID);
                    h.Note = hnote != null ? hnote.Title : "Праздник";

                    res.Add(h);

                    continue;
                }

#pragma warning disable IDE0078 // Используйте сопоставление шаблонов
                if (date.DayOfWeek == DayOfWeek.Sunday || date.DayOfWeek == DayOfWeek.Saturday)
#pragma warning restore IDE0078 // Используйте сопоставление шаблонов
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

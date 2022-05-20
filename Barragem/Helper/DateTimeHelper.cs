using System;
using System.Globalization;

namespace Barragem.Helper
{
    public static class DateTimeHelper
    {
        public static string GetMonthName(this DateTime date)
        {
            CultureInfo culture = new CultureInfo("pt-BR");
            DateTimeFormatInfo dtfi = culture.DateTimeFormat;

            return culture.TextInfo.ToTitleCase(dtfi.GetMonthName(date.Month));
        }

        public static DateTime GetDateTimeBrasilia() 
        {
            DateTime dateTime = DateTime.UtcNow;
            TimeZoneInfo horaBrasilia = TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time");
            return TimeZoneInfo.ConvertTimeFromUtc(dateTime, horaBrasilia);
        }

    }
}
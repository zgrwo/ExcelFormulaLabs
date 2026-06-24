using System;
using System.Globalization;
using ExcelVbaLibraries.Foundation;

namespace ExcelVbaLibraries.DataToolkit
{
    /// <summary>ISO week, workdays, age, holidays, Easter. Ported from DateTimeUtils.bas.</summary>
    internal static class DateTimeCore
    {
        internal static long IsoWeekNum(DateTime d)
        {
#if NET48
            // ISO 8601: week starts Monday, week 1 contains Jan 4
            int dow = (int)d.DayOfWeek; if (dow == 0) dow = 7;
            var thu = d.AddDays(4 - dow);
            var jan1 = new DateTime(thu.Year, 1, 1);
            int jdow = (int)jan1.DayOfWeek; if (jdow == 0) jdow = 7;
            return ((thu - jan1).Days + jdow - 1) / 7 + 1;
#else
            return ISOWeek.GetWeekOfYear(d);
#endif
        }
        internal static long IsoYear(DateTime d)
        {
#if NET48
            int dow = (int)d.DayOfWeek; if (dow == 0) dow = 7;
            return d.AddDays(4 - dow).Year;
#else
            return ISOWeek.GetYear(d);
#endif
        }
        internal static long Weekday(DateTime d) => (long)d.DayOfWeek + 1;
        internal static long WeekdayISO(DateTime d) => d.DayOfWeek == DayOfWeek.Sunday ? 7 : (long)d.DayOfWeek;
        internal static string WeekdayName(DateTime d) => d.ToString("dddd", CultureInfo.InvariantCulture);
        internal static DateTime StartOfWeek(DateTime d, DayOfWeek s = DayOfWeek.Monday) { int df = (7 + (int)d.DayOfWeek - (int)s) % 7; return d.Date.AddDays(-df); }
        internal static DateTime EndOfWeek(DateTime d, DayOfWeek s = DayOfWeek.Monday) => StartOfWeek(d, s).AddDays(6);
        internal static DateTime StartOfMonth(DateTime d) => new(d.Year, d.Month, 1);
        internal static DateTime EndOfMonth(DateTime d) => new(d.Year, d.Month, DateTime.DaysInMonth(d.Year, d.Month));
        internal static long WeekOfMonth(DateTime d, DayOfWeek s = DayOfWeek.Monday) { long w = 0; var c = StartOfMonth(d); while (c <= d) { if (c.DayOfWeek == s) w++; c = c.AddDays(1); } return w; }
        internal static long AgeYears(DateTime b, DateTime? r = null) { AssertValidDate(b); var rd = r ?? DateTime.Today; int a = rd.Year - b.Year; if (rd.Month < b.Month || (rd.Month == b.Month && rd.Day < b.Day)) a--; return a; }
        internal static long AgeMonths(DateTime b, DateTime? r = null) { AssertValidDate(b); var rd = r ?? DateTime.Today; return (rd.Year - b.Year) * 12 + rd.Month - b.Month - (rd.Day < b.Day ? 1 : 0); }
        internal static long AgeDays(DateTime b, DateTime? r = null) { AssertValidDate(b); return (long)((r ?? DateTime.Today) - b).TotalDays; }
        internal static bool IsWeekend(DateTime d) => d.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
        internal static DateTime AddWorkdays(DateTime s, long days) { var d = s; long inc = days > 0 ? 1 : -1; long rem = Math.Abs(days); while (rem > 0) { d = d.AddDays(inc); if (!IsWeekend(d)) rem--; } return d; }
        internal static long WorkdaysBetween(DateTime s, DateTime e) { long w = 0; var d = s.AddDays(1); while (d <= e) { if (!IsWeekend(d)) w++; d = d.AddDays(1); } return w; }
        internal static DateTime NextWorkday(DateTime d) { d = d.AddDays(1); while (IsWeekend(d)) d = d.AddDays(1); return d; }
        internal static DateTime Easter(long y) { long a = y % 19, b = y / 100, c = y % 100, d = b / 4, e = b % 4, f = (b + 8) / 25, g = (b - f + 1) / 3, h = (19 * a + b - d - g + 15) % 30, i = c / 4, k = c % 4, l = (32 + 2 * e + 2 * i - h - k) % 7, m = (a + 11 * h + 22 * l) / 451; long mo = (h + l - 7 * m + 114) / 31; long da = (h + l - 7 * m + 114) % 31 + 1; return new DateTime((int)y, (int)mo, (int)da); }
        internal static long Quarter(DateTime d) => (d.Month + 2) / 3;
        internal static long Semester(DateTime d) => (d.Month + 5) / 6;
        internal static long DayOfYear(DateTime d) => d.DayOfYear;
        internal static bool IsLeapYear(long y) => DateTime.IsLeapYear((int)y);
        internal static long DaysInMonth(long y, long m) => DateTime.DaysInMonth((int)y, (int)m);
        internal static double UnixTimestamp(DateTime d) => (d.ToUniversalTime() - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
        internal static DateTime FromUnixTimestamp(double ts) => new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(ts).ToLocalTime();
        internal static long DateDiff(string u, DateTime d1, DateTime d2) { AssertValidDate(d1); AssertValidDate(d2); return u.ToUpperInvariant() switch
        {
            "D" or "DAY" => (long)(d2 - d1).TotalDays,
            "M" or "MONTH" => (d2.Year - d1.Year) * 12 + d2.Month - d1.Month,
            "Y" or "YEAR" => d2.Year - d1.Year - (d2.Month < d1.Month || (d2.Month == d1.Month && d2.Day < d1.Day) ? 1 : 0),
            "W" or "WEEK" => (long)(d2 - d1).TotalDays / 7,
            _ => throw new ArgumentException($"Unknown date unit '{u}'. Supported units: D (days), M (months), Y (years), W (weeks).")
        }; }

        private static void AssertValidDate(DateTime d)
        { if (d == DateTime.MinValue || d == DateTime.MaxValue) throw new ArgumentException("Invalid date (likely from empty/error cell)."); }
    }
}

using System;
using ExcelDna.Integration;
using ExcelVbaLibraries.Foundation;

namespace ExcelVbaLibraries.DataToolkit
{
    public static class DateTimeUdf
    {
        private static DateTime D(object d)=>DateTime.FromOADate(InputNormalizer.ToDouble(d));
        [ExcelFunction(Name="DT.ISOWEEK", Description="ISO 8601 week number (1-53) from an Excel date")] public static object UDF_DT_ISOW(object d)=>OutputWrapper.WrapError(()=>ElementWiseMapper.MapOver<double,long>(d,x=>DateTimeCore.IsoWeekNum(D(x))));
        [ExcelFunction(Name="DT.WEEKDAY", Description="Day of week as number (Sunday=0, Saturday=6)")] public static object UDF_DT_WKD(object d)=>OutputWrapper.WrapError(()=>ElementWiseMapper.MapOver<double,long>(d,x=>DateTimeCore.Weekday(D(x))));
        [ExcelFunction(Name="DT.WEEKDAYISO", Description="Day of week as ISO number (Monday=1, Sunday=7)")] public static object UDF_DT_WKDISO(object d)=>OutputWrapper.WrapError(()=>ElementWiseMapper.MapOver<double,long>(d,x=>DateTimeCore.WeekdayISO(D(x))));
        [ExcelFunction(Name="DT.WEEKDAYNAME", Description="English weekday name (e.g. 'Monday') from an Excel date")] public static object UDF_DT_WKDNAM(object d)=>OutputWrapper.WrapError(()=>ElementWiseMapper.MapOver<double,string>(d,x=>DateTimeCore.WeekdayName(D(x))));
        [ExcelFunction(Name="DT.SOW", Description="Start of week date. sd = start day (0=Sun, 1=Mon, ...)")] public static object UDF_DT_SOW(object d,object sd)=>OutputWrapper.WrapError(()=>ElementWiseMapper.MapOver<double,double>(d,x=>DateTimeCore.StartOfWeek(D(x),(DayOfWeek)(int)InputNormalizer.ToLong(sd)).ToOADate()));
        [ExcelFunction(Name="DT.EOW", Description="End of week date. sd = start day (0=Sun, 1=Mon, ...)")] public static object UDF_DT_EOW(object d,object sd)=>OutputWrapper.WrapError(()=>ElementWiseMapper.MapOver<double,double>(d,x=>DateTimeCore.EndOfWeek(D(x),(DayOfWeek)(int)InputNormalizer.ToLong(sd)).ToOADate()));
        [ExcelFunction(Name="DT.SOM", Description="Start of month date (1st day of month)")] public static object UDF_DT_SOM(object d)=>OutputWrapper.WrapError(()=>ElementWiseMapper.MapOver<double,double>(d,x=>DateTimeCore.StartOfMonth(D(x)).ToOADate()));
        [ExcelFunction(Name="DT.EOM", Description="End of month date (last day of month)")] public static object UDF_DT_EOM(object d)=>OutputWrapper.WrapError(()=>ElementWiseMapper.MapOver<double,double>(d,x=>DateTimeCore.EndOfMonth(D(x)).ToOADate()));
        [ExcelFunction(Name="DT.WOM", Description="Week-of-month ordinal (1-5). sd = start day (0=Sun, ...)")] public static object UDF_DT_WOM(object d,object sd)=>OutputWrapper.WrapError(()=>(long)DateTimeCore.WeekOfMonth(D(d),(DayOfWeek)(int)InputNormalizer.ToLong(sd)));
        [ExcelFunction(Name="DT.DIM", Description="Days in month for given year and month")] public static object UDF_DT_DIM(object y,object m)=>OutputWrapper.WrapError(()=>(long)DateTimeCore.DaysInMonth(InputNormalizer.ToLong(y),InputNormalizer.ToLong(m)));
        [ExcelFunction(Name="DT.AGEYEARS", Description="Age in whole years from birth date to reference date (default: today)")] public static object UDF_DT_AGEY(object b,object r)=>OutputWrapper.WrapError(()=>(long)DateTimeCore.AgeYears(D(b),InputNormalizer.ToDouble(r)>0?D(r):null));
        [ExcelFunction(Name="DT.AGEMONTHS", Description="Age in whole months from birth date to reference date (default: today)")] public static object UDF_DT_AGEM(object b,object r)=>OutputWrapper.WrapError(()=>(long)DateTimeCore.AgeMonths(D(b),InputNormalizer.ToDouble(r)>0?D(r):null));
        [ExcelFunction(Name="DT.AGEDAYS", Description="Age in days from birth date to reference date (default: today)")] public static object UDF_DT_AGED(object b,object r)=>OutputWrapper.WrapError(()=>(long)DateTimeCore.AgeDays(D(b),InputNormalizer.ToDouble(r)>0?D(r):null));
        [ExcelFunction(Name="DT.ISWE", Description="Returns TRUE if date is Saturday or Sunday")] public static object UDF_DT_ISWE(object d)=>OutputWrapper.WrapError(()=>ElementWiseMapper.MapOver<double,bool>(d,x=>DateTimeCore.IsWeekend(D(x))));
        [ExcelFunction(Name="DT.ADDWKD", Description="Add n workdays to a start date (Mon-Fri, skips weekends)")] public static object UDF_DT_AWKD(object s,object ds)=>OutputWrapper.WrapError(()=>DateTimeCore.AddWorkdays(D(s),InputNormalizer.ToLong(ds)).ToOADate());
        [ExcelFunction(Name="DT.WKDBTWN", Description="Count workdays (Mon-Fri) between two dates, exclusive of start")] public static object UDF_DT_WKDB(object s,object e)=>OutputWrapper.WrapError(()=>(long)DateTimeCore.WorkdaysBetween(D(s),D(e)));
        [ExcelFunction(Name="DT.NEXTWKD", Description="Next workday on or after given date (skips weekends)")] public static object UDF_DT_NWKD(object d)=>OutputWrapper.WrapError(()=>DateTimeCore.NextWorkday(D(d)).ToOADate());
        [ExcelFunction(Name="DT.EASTER", Description="Easter Sunday date for given year (Gregorian computus)")] public static object UDF_DT_EASTER(object y)=>OutputWrapper.WrapError(()=>DateTimeCore.Easter(InputNormalizer.ToLong(y)).ToOADate());
        [ExcelFunction(Name="DT.QUARTER", Description="Calendar quarter number (1-4) from date")] public static object UDF_DT_Q(object d)=>OutputWrapper.WrapError(()=>ElementWiseMapper.MapOver<double,long>(d,x=>DateTimeCore.Quarter(D(x))));
        [ExcelFunction(Name="DT.SEMESTER", Description="Semester number (1 or 2) from date")] public static object UDF_DT_SEM(object d)=>OutputWrapper.WrapError(()=>ElementWiseMapper.MapOver<double,long>(d,x=>DateTimeCore.Semester(D(x))));
        [ExcelFunction(Name="DT.DOY", Description="Day of year (1-366) from date")] public static object UDF_DT_DOY(object d)=>OutputWrapper.WrapError(()=>ElementWiseMapper.MapOver<double,long>(d,x=>DateTimeCore.DayOfYear(D(x))));
        [ExcelFunction(Name="DT.ISLEAP", Description="Returns TRUE if year is a leap year")] public static object UDF_DT_ILEAP(object y)=>OutputWrapper.WrapError(()=>(object)DateTimeCore.IsLeapYear(InputNormalizer.ToLong(y)));
        [ExcelFunction(Name="DT.UNIXTS", Description="Convert Excel date to Unix timestamp (seconds since 1970-01-01)")] public static object UDF_DT_UXTS(object d)=>OutputWrapper.WrapError(()=>DateTimeCore.UnixTimestamp(D(d)));
        [ExcelFunction(Name="DT.FROMUNIX", Description="Convert Unix timestamp (seconds) to Excel date serial")] public static object UDF_DT_FUXTS(object ts)=>OutputWrapper.WrapError(()=>DateTimeCore.FromUnixTimestamp(InputNormalizer.ToDouble(ts)).ToOADate());
        [ExcelFunction(Name="DT.DATEDIFF", Description="Date difference in units: 'd'=days, 'm'=months, 'y'=years, 'w'=weeks")] public static object UDF_DT_DDIFF(object u,object d1,object d2)=>OutputWrapper.WrapError(()=>(long)DateTimeCore.DateDiff(InputNormalizer.ToString(u),D(d1),D(d2)));
    }
}

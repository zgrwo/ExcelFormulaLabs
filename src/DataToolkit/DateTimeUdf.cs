using System;
using ExcelDna.Integration;
using ExcelVbaLibraries.Foundation;

namespace ExcelVbaLibraries.DataToolkit
{
    public static class DateTimeUdf
    {
        private static DateTime D(object d)=>DateTime.FromOADate(InputNormalizer.ToDouble(d));
        [ExcelFunction(Name="DT.ISOWEEK")] public static object UDF_DT_ISOW(object d)=>OutputWrapper.WrapError(()=>ElementWiseMapper.MapOver<double,long>(d,x=>DateTimeCore.IsoWeekNum(D(x))));
        [ExcelFunction(Name="DT.WEEKDAY")] public static object UDF_DT_WKD(object d)=>OutputWrapper.WrapError(()=>ElementWiseMapper.MapOver<double,long>(d,x=>DateTimeCore.Weekday(D(x))));
        [ExcelFunction(Name="DT.WEEKDAYISO")] public static object UDF_DT_WKDISO(object d)=>OutputWrapper.WrapError(()=>ElementWiseMapper.MapOver<double,long>(d,x=>DateTimeCore.WeekdayISO(D(x))));
        [ExcelFunction(Name="DT.WEEKDAYNAME")] public static object UDF_DT_WKDNAM(object d)=>OutputWrapper.WrapError(()=>ElementWiseMapper.MapOver<double,string>(d,x=>DateTimeCore.WeekdayName(D(x))));
        [ExcelFunction(Name="DT.SOW")] public static object UDF_DT_SOW(object d,object sd)=>OutputWrapper.WrapError(()=>ElementWiseMapper.MapOver<double,double>(d,x=>DateTimeCore.StartOfWeek(D(x),(DayOfWeek)(int)InputNormalizer.ToLong(sd)).ToOADate()));
        [ExcelFunction(Name="DT.EOW")] public static object UDF_DT_EOW(object d,object sd)=>OutputWrapper.WrapError(()=>ElementWiseMapper.MapOver<double,double>(d,x=>DateTimeCore.EndOfWeek(D(x),(DayOfWeek)(int)InputNormalizer.ToLong(sd)).ToOADate()));
        [ExcelFunction(Name="DT.SOM")] public static object UDF_DT_SOM(object d)=>OutputWrapper.WrapError(()=>ElementWiseMapper.MapOver<double,double>(d,x=>DateTimeCore.StartOfMonth(D(x)).ToOADate()));
        [ExcelFunction(Name="DT.EOM")] public static object UDF_DT_EOM(object d)=>OutputWrapper.WrapError(()=>ElementWiseMapper.MapOver<double,double>(d,x=>DateTimeCore.EndOfMonth(D(x)).ToOADate()));
        [ExcelFunction(Name="DT.WOM")] public static object UDF_DT_WOM(object d,object sd)=>OutputWrapper.WrapError(()=>(long)DateTimeCore.WeekOfMonth(D(d),(DayOfWeek)(int)InputNormalizer.ToLong(sd)));
        [ExcelFunction(Name="DT.DIM")] public static object UDF_DT_DIM(object y,object m)=>OutputWrapper.WrapError(()=>(long)DateTimeCore.DaysInMonth(InputNormalizer.ToLong(y),InputNormalizer.ToLong(m)));
        [ExcelFunction(Name="DT.AGEYEARS")] public static object UDF_DT_AGEY(object b,object r)=>OutputWrapper.WrapError(()=>(long)DateTimeCore.AgeYears(D(b),InputNormalizer.ToDouble(r)>0?D(r):null));
        [ExcelFunction(Name="DT.AGEMONTHS")] public static object UDF_DT_AGEM(object b,object r)=>OutputWrapper.WrapError(()=>(long)DateTimeCore.AgeMonths(D(b),InputNormalizer.ToDouble(r)>0?D(r):null));
        [ExcelFunction(Name="DT.AGEDAYS")] public static object UDF_DT_AGED(object b,object r)=>OutputWrapper.WrapError(()=>(long)DateTimeCore.AgeDays(D(b),InputNormalizer.ToDouble(r)>0?D(r):null));
        [ExcelFunction(Name="DT.ISWE")] public static object UDF_DT_ISWE(object d)=>OutputWrapper.WrapError(()=>ElementWiseMapper.MapOver<double,bool>(d,x=>DateTimeCore.IsWeekend(D(x))));
        [ExcelFunction(Name="DT.ADDWKD")] public static object UDF_DT_AWKD(object s,object ds)=>OutputWrapper.WrapError(()=>DateTimeCore.AddWorkdays(D(s),InputNormalizer.ToLong(ds)).ToOADate());
        [ExcelFunction(Name="DT.WKDBTWN")] public static object UDF_DT_WKDB(object s,object e)=>OutputWrapper.WrapError(()=>(long)DateTimeCore.WorkdaysBetween(D(s),D(e)));
        [ExcelFunction(Name="DT.NEXTWKD")] public static object UDF_DT_NWKD(object d)=>OutputWrapper.WrapError(()=>DateTimeCore.NextWorkday(D(d)).ToOADate());
        [ExcelFunction(Name="DT.EASTER")] public static object UDF_DT_EASTER(object y)=>OutputWrapper.WrapError(()=>DateTimeCore.Easter(InputNormalizer.ToLong(y)).ToOADate());
        [ExcelFunction(Name="DT.QUARTER")] public static object UDF_DT_Q(object d)=>OutputWrapper.WrapError(()=>ElementWiseMapper.MapOver<double,long>(d,x=>DateTimeCore.Quarter(D(x))));
        [ExcelFunction(Name="DT.SEMESTER")] public static object UDF_DT_SEM(object d)=>OutputWrapper.WrapError(()=>ElementWiseMapper.MapOver<double,long>(d,x=>DateTimeCore.Semester(D(x))));
        [ExcelFunction(Name="DT.DOY")] public static object UDF_DT_DOY(object d)=>OutputWrapper.WrapError(()=>ElementWiseMapper.MapOver<double,long>(d,x=>DateTimeCore.DayOfYear(D(x))));
        [ExcelFunction(Name="DT.ISLEAP")] public static object UDF_DT_ILEAP(object y)=>OutputWrapper.WrapError(()=>(object)DateTimeCore.IsLeapYear(InputNormalizer.ToLong(y)));
        [ExcelFunction(Name="DT.UNIXTS")] public static object UDF_DT_UXTS(object d)=>OutputWrapper.WrapError(()=>DateTimeCore.UnixTimestamp(D(d)));
        [ExcelFunction(Name="DT.FROMUNIX")] public static object UDF_DT_FUXTS(object ts)=>OutputWrapper.WrapError(()=>DateTimeCore.FromUnixTimestamp(InputNormalizer.ToDouble(ts)).ToOADate());
        [ExcelFunction(Name="DT.DATEDIFF")] public static object UDF_DT_DDIFF(object u,object d1,object d2)=>OutputWrapper.WrapError(()=>(long)DateTimeCore.DateDiff(InputNormalizer.ToString(u),D(d1),D(d2)));
    }
}

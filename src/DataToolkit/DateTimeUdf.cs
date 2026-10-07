using System;
using ExcelDna.Integration;
using ExcelFormulaLabs.Foundation;

namespace ExcelFormulaLabs.DataToolkit
{
    public static partial class DateTimeUdf
    {
        // Excel 序列 1–59 经 OADate 比真实 Excel 显示值早一天（1900 假闰年），且 60 与 59 同值。
        // 按产品决策：1–59 统一 +1 对齐 Excel 显示；60（不存在的 1900-02-29）显式 #VALUE!
        // 而非静默给 1900-02-28。序列 ≤0 / ≥61 保持 OADate 语义不变。
        private static DateTime D(object d)
        {
            double v = InputNormalizer.ToDouble(d);
            if (double.IsNaN(v)) throw new ArgumentException(ErrorMsg.Get("DT_NaNTodate"));
            // 假闰日区间是 [60,61)（R3-5）：旧的 `v == 60` 只挡整数 60，60.5 经
            // FromOADate 静默映射 1900-02-28 12:00（与契约注释"显式 #VALUE!"相悖）。
            if (v >= 60 && v < 61) throw new ArgumentException("Excel serial 60 (the fictitious 1900-02-29) is not a valid date.");
            if (v >= 1 && v < 60) v += 1;
            return DateTime.FromOADate(v);
        }
        // Week start-day adapter: default 1=Monday; validates 0-6 range so out-of-range
        // values surface as #VALUE! instead of silently producing wrong dates.
        private static DayOfWeek SD(object sd){long v=InputNormalizer.IsOmitted(sd)?1L:InputNormalizer.ToLong(sd);if(v<0||v>6)throw new ArgumentException("start_day must be between 0 (Sunday) and 6 (Saturday).");return (DayOfWeek)(int)v;}
        // 可选日期参数「未提供」只认 null/ExcelMissing/DBNull/ExcelEmpty。
        // `ToDouble(r)>0?D(r):null` 会把合法序列号 0（1899-12-30）与 NaN（文本/区域输入）都
        // 吞成「默认今天」——静默错值。已提供但不可转换 → D() 抛错 → #VALUE!（与必选参数同语义）。
        private static DateTime? OptD(object d)=>d==null||d is ExcelDna.Integration.ExcelMissing||d is DBNull||InputNormalizer.IsExcelEmptyValue(d)?(DateTime?)null:D(d);
        // Core Easter 支持 1–9999，但 UDF 输出 OADate 无法表示 100 年以前（DateTime.ToOADate
        // 抛错）→ 1–99 会静默 #VALUE!，与文档矛盾。显式限定 100–9999 并给出原因
        // （Excel OLE 日期可表示范围）。
        private static double EasterOADate(long yr)
        {
            if (yr < 100 || yr > 9999)
                throw new ArgumentException(
                    $"EASTER year {yr} is outside the OLE-date representable range [100, 9999].");
            return DateTimeCore.Easter(yr).ToOADate();
        }
    }
}

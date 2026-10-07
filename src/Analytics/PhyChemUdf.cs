using ExcelDna.Integration;
using ExcelFormulaLabs.Foundation;

namespace ExcelFormulaLabs.Analytics
{
    public static partial class PhyChemUdf
    {
        // 错误输入原样传播（同 MapOver 系列），数组/标量口径一致；
        // t_unit/p_unit 遵循 snake_case 参数规范。
        private static string S(object o)=>InputNormalizer.ToString(o);
        // Excel 错误/非 "*" 文本不得静默当作"待求量"（#REF! 输入会返回貌似合理的解）：
        // 错误值与非占位文本必须显式失败（→ #VALUE!），只有数值、空白与 "*" 占位参与求解。
        private static double? V(object o)
        {
            if (o == null || InputNormalizer.IsExcelEmptyValue(o)) return null;
            if (InputNormalizer.IsExcelErrorValue(o))
                throw new System.ArgumentException(
                    "Ideal gas parameter is an Excel error value; fix the reference before solving.");
            if (o is string s)
            {
                if (s == "*") return null;
                throw new System.ArgumentException(
                    "Ideal gas parameter must be numeric or \"*\" to mark the unknown quantity.");
            }
            double d = InputNormalizer.ToDouble(o);
            if (double.IsNaN(d))
                throw new System.ArgumentException(
                    "Ideal gas parameter is not numeric; pass a number or \"*\".");
            return d;
        }
    }
}

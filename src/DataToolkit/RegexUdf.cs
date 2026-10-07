using System;
using System.Runtime.InteropServices;
using ExcelDna.Integration;
using ExcelFormulaLabs.Foundation;

namespace ExcelFormulaLabs.DataToolkit
{
    public static partial class RegexUdf
    {
        // R1-4：每格 5s Timeout 在数组分发下线性放大（N 格 = N×5s CPU）。Budgeted 为
        // 一次 UDF 调用建立数组级墙钟预算（Foundation.RegexBudget）：RegexCore 每次
        // 操作取 min(单次 5s, 剩余预算)，耗尽即抛 → WrapError → #VALUE!，总耗时 ≤ 预算 + 单格。
        private static object Budgeted(Func<object> f)
        {
            using (RegexBudget.Begin()) return f();
        }

    }
}

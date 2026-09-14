using System;
using System.Collections.Generic;
using ExcelFormulaLabs.Foundation;

namespace ExcelFormulaLabs.DataToolkit.Tests
{
    /// <summary>review 2026-09-14（P1 UDF-01）：可选参数「未提供」的 4 类哨兵 + ExcelEmpty。
    /// UDF 守卫必须把这些全部视为省略并回退文档默认值（与 null 一致）。</summary>
    internal static class OmittedSentinelData
    {
        public static IEnumerable<object?[]> All
        {
            get
            {
                yield return new object?[] { null };
                yield return new object?[] { DBNull.Value };
                yield return new object?[] { ExcelEmpty.Value };
                yield return new object?[] { ExcelDna.Integration.ExcelEmpty.Value };
                yield return new object?[] { ExcelDna.Integration.ExcelMissing.Value };
            }
        }
    }
}

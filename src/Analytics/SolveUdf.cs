using System;
using ExcelDna.Integration;
using ExcelFormulaLabs.Foundation;

namespace ExcelFormulaLabs.Analytics
{
    /// <summary>
    /// SOLVE.* UDF wrappers: process-parameter inversion, forward prediction, model quality
    /// and equation text. Delegates all logic to <see cref="SolveCore"/> (UDF layer only
    /// marshals arguments and wraps errors).
    /// </summary>
    public static partial class SolveUdf
    {

        private static object[,] RequiredTable(object value, string name)
        {
            // 统一走 IsOmitted（P2-4）：旧实现手写 null/Missing/Empty 三态，漏掉 DBNull 分支。
            if (InputNormalizer.IsOmitted(value))
                throw new ArgumentException($"'{name}' must be a range or array, not empty.");
            return InputNormalizer.NormalizeTo2D(value)
                ?? throw new ArgumentException($"'{name}' must be a 2D range or array.");
        }

        private static object[,]? OptionalTable(object value)
        {
            // 同 RequiredTable：统一 IsOmitted（P2-4）。
            if (InputNormalizer.IsOmitted(value))
                return null;
            return InputNormalizer.NormalizeTo2D(value);
        }
    }
}

using System;
using System.Diagnostics;

namespace ExcelFormulaLabs.Foundation
{
    /// <summary>
    /// Error-safe execution wrapper and output reshaping utilities.
    /// Every UDF wrapper delegates to <see cref="WrapError"/> to ensure
    /// that exceptions become <c>#VALUE!</c> rather than crashing Excel.
    /// </summary>
    /// <remarks>
    /// In VBA, every UDF has boilerplate:
    /// <code>
    ///   On Error GoTo EH
    ///   UDF_XXX = ...core logic...
    ///   Exit Function
    /// EH:
    ///   UDF_XXX = CVErr(xlErrValue)
    /// End Function
    /// </code>
    /// In C# with Foundation, this becomes:
    /// <code>
    ///   OutputWrapper.WrapError(() => ...core logic...);
    /// </code>
    /// </remarks>
    public static class OutputWrapper
    {
        /// <summary>
        /// Execute <paramref name="action"/>; if it throws, return <see cref="ExcelError.Value"/>.
        /// </summary>
        /// <param name="action">The core logic, returning <c>object</c>.</param>
        /// <returns>The action's result, or <c>#VALUE!</c> on exception.</returns>
        public static object WrapError(Func<object> action)
        {
            try { return action(); }
            catch (Exception ex) when (ExceptionFilters.IsCatchable(ex)) { var msg = $"[WrapError] {ex.GetType().Name}"; Debug.WriteLine(msg); Trace.WriteLine(msg); return ExcelError.Value; }
        }

        /// <summary>
        /// Type-safe variant: execute and return typed value.
        /// On exception, return <paramref name="errorResult"/>.
        /// </summary>
        public static T WrapError<T>(Func<T> action, T errorResult)
        {
            try { return action(); }
            catch (Exception ex) when (ExceptionFilters.IsCatchable(ex)) { var msg = $"[WrapError] {ex.GetType().Name}"; Debug.WriteLine(msg); Trace.WriteLine(msg); return errorResult; }
        }

        /// <summary>
        /// Reshape a flat result into a 2D array matching target dimensions.
        /// Pads with <c>null</c> (empty cell) if result is too short;
        /// truncates if too long.
        /// null 填充（替代 Foundation.ExcelEmpty.Value）：后者不在 Excel-DNA 封送白名单内，
        /// 真实 Excel 渲染为 #NUM!；Excel-DNA 对 object[,] 中的 null 渲染为空单元格。
        /// 负数尺寸显式拒绝、零尺寸返回空网格；object[,] 按目标尺寸逐格拷贝/裁剪
        /// （直接写 output[0,0] 会在 0×0 目标越界且 targetCols=0 时整数除零）。
        /// </summary>
        /// <exception cref="ArgumentException">targetRows/targetCols 为负。</exception>
        public static object[,] ReshapeOutput(object result, int targetRows, int targetCols)
        {
            if (targetRows < 0 || targetCols < 0)
                throw new ArgumentException(
                    $"ReshapeOutput target dimensions must be non-negative (got {targetRows}×{targetCols}).");
            var output = new object[targetRows, targetCols];
            if (result == null || targetRows == 0 || targetCols == 0) return output;

            if (result is object[,] grid)
            {
                int rows = Math.Min(targetRows, grid.GetLength(0));
                int cols = Math.Min(targetCols, grid.GetLength(1));
                for (int r = 0; r < rows; r++)
                    for (int c = 0; c < cols; c++)
                        output[r, c] = grid[r, c] ?? null!;
                return output;
            }

            if (result is object[] flat)
            {
                int count = Math.Min(flat.Length, targetRows * targetCols);
                for (int i = 0; i < count; i++)
                    output[i / targetCols, i % targetCols] = flat[i] ?? null!;
                return output;
            }

            output[0, 0] = result;
            return output;
        }
    }
}

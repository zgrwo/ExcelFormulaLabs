using System;

namespace ExcelFormulaLabs.Foundation
{
    /// <summary>
    /// Element-wise mapping over scalar or array inputs — the core abstraction
    /// that eliminates ~3000 lines of duplicated boilerplate across 240 UDF wrappers.
    /// </summary>
    /// <remarks>
    /// <para>
    /// In the VBA codebase, each UDF manually handles:
    /// </para>
    /// <list type="number">
    ///   <item>COM Range detection and <c>.Value</c> extraction</item>
    ///   <item>Array shape probing (scalar vs 1D vs 2D)</item>
    ///   <item>Element-wise iteration with <c>For i/For j</c> loops</item>
    ///   <item>Error/Null/Empty propagation (special markers pass through unchanged)</item>
    ///   <item>Type coercion before calling core logic</item>
    /// </list>
    ///
    /// <para>
    /// <b>MapOver</b> centralises all five steps. A UDF becomes:
    /// <code>
    ///   OutputWrapper.WrapError(() => MapOver(input, Core.ReverseString));
    /// </code>
    /// </para>
    ///
    /// <para><b>Shape preservation:</b> scalar→scalar, 1D→1D, 2D→2D (same dimensions).</para>
    /// <para><b>Broadcasting:</b> multi-argument overloads broadcast scalar args to match array dimensions.</para>
    /// </remarks>
    public static class ElementWiseMapper
    {
        // ─────────────────────────────────────────────────────────────────
        // Single-argument mapping (90% of UDFs)
        // ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// Map a scalar or array input element-wise, preserving shape.
        /// </summary>
        /// <typeparam name="TInput">Target type for each cell (e.g. string, double).</typeparam>
        /// <typeparam name="TOutput">Return type from mapper (e.g. string, long, bool).</typeparam>
        /// <param name="input">Raw input from Excel: Range, array, or scalar.</param>
        /// <param name="mapper">Pure function mapping a typed cell value to a typed result.</param>
        /// <returns>
        /// <c>object</c> result — scalar→scalar, array→same-shape array.
        /// Error/Null/Empty cells are propagated through unchanged.
        /// </returns>
        public static object MapOver<TInput, TOutput>(
            object input, Func<TInput, TOutput> mapper)
        {
            // Step 1: COM Range → array
            InputNormalizer.TryExtractComRangeValue(input, out input);

            // Step 2: Branch by shape
            if (input is object[,] arr2D)
                return Map2D(arr2D, mapper);

            if (input is object[] arr1D)
                return Map1D(arr1D, mapper);

            // typed arrays (double[], int[,], …) arrive from direct .NET callers; treating
            // them as a scalar cell would collapse to a single NaN. Route through the
            // normalizer (which handles typed arrays) for element-wise mapping, consistent
            // with MapOverFlat/NormalizeTo1D behaviour.
            if (input is Array typed)
                return Map1D(InputNormalizer.NormalizeTo1D(typed), mapper);

            // Step 3: Scalar
            return MapSingleCell(input, mapper);
        }

        /// <summary>
        /// Map element-wise, always producing a 1D array regardless of input shape.
        /// Use for ArrayUtils-style functions (ArraySort, ArrayUnique, etc.).
        /// </summary>
        public static object[] MapOverFlat<TInput, TOutput>(
            object input, Func<TInput, TOutput> mapper)
        {
            InputNormalizer.TryExtractComRangeValue(input, out input);
            object[] flat = InputNormalizer.NormalizeTo1D(input);
            var result = new object[flat.Length];
            for (int i = 0; i < flat.Length; i++)
                result[i] = MapSingleCell(flat[i], mapper);
            return result;
        }

        // ─────────────────────────────────────────────────────────────────
        // Multi-argument mapping with broadcasting
        // ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// Map over two inputs element-wise with broadcasting.
        /// Scalars broadcast to match array size.
        /// Mismatched array sizes return <see cref="ExcelError.Value"/>.
        /// </summary>
        public static object MapOverMulti<T1, T2, TOutput>(
            object input1, object input2, Func<T1, T2, TOutput> mapper)
        {
            InputNormalizer.TryExtractComRangeValue(input1, out input1);
            InputNormalizer.TryExtractComRangeValue(input2, out input2);

            object[] flat1 = InputNormalizer.NormalizeTo1D(input1);
            object[] flat2 = InputNormalizer.NormalizeTo1D(input2);
            // null 标量输入应广播（视为 [null]）而非被 NormalizeTo1D(null)=空数组 吞掉——
            // 否则 MapOverMulti(null, ["a","b"]) 直接返回空结果。
            if (input1 == null) flat1 = new object[] { null! };
            if (input2 == null) flat2 = new object[] { null! };

            // was2D is checked AFTER TryExtractComRangeValue (line 92-93) because
            // COM Range extraction converts the input to its .Value (object[,] for
            // multi-cell ranges), so the post-extraction check correctly detects
            // both native object[,] and COM-Range-originated 2D arrays.
            bool was2D = input1 is object[,] || input2 is object[,];
            // 等元素数不同形状（[2,3] vs [3,2]）须在映射前预校验：否则
            // ReshapeFlatToOriginal2D 抛 InvalidOperationException，违背
            // "尺寸不匹配 → ExcelError.Value" 契约。
            if (was2D && HasMismatched2DShapes(input1!, input2!))
                return ExcelError.Value;

            // 任一输入展平为空 → 返回 null（而非 ExcelError）——空区域广播无目标单元格，
            // UDF 层把 null 渲染为空白。
            if (flat1.Length == 0 || flat2.Length == 0)
                return null!;

            if (flat1.Length == 1 && flat2.Length == 1)
                return was2D
                    ? new object[,] { { MapSingleCell(flat1[0], flat2[0], mapper) } }
                    : MapSingleCell(flat1[0], flat2[0], mapper);

            if (flat1.Length == 1)
                return MapMultiBroadcast(flat1[0], flat2, mapper, was2D, input1!, input2!);

            if (flat2.Length == 1)
                return PreserveShape2D(flat1, flat2[0], mapper, was2D, input1!, input2!);

            if (flat1.Length == flat2.Length)
                return MapMultiSameLength(flat1, flat2, mapper, was2D, input1!, input2!);

            return ExcelError.Value;
        }

        /// <summary>
        /// Map over three inputs element-wise with broadcasting.
        /// </summary>
        public static object MapOverMulti<T1, T2, T3, TOutput>(
            object input1, object input2, object input3,
            Func<T1, T2, T3, TOutput> mapper)
        {
            InputNormalizer.TryExtractComRangeValue(input1, out input1);
            InputNormalizer.TryExtractComRangeValue(input2, out input2);
            InputNormalizer.TryExtractComRangeValue(input3, out input3);

            object[] flat1 = InputNormalizer.NormalizeTo1D(input1);
            object[] flat2 = InputNormalizer.NormalizeTo1D(input2);
            object[] flat3 = InputNormalizer.NormalizeTo1D(input3);
            // null 标量广播（同 2 参数版本）。
            if (input1 == null) flat1 = new object[] { null! };
            if (input2 == null) flat2 = new object[] { null! };
            if (input3 == null) flat3 = new object[] { null! };

            bool was2D = input1 is object[,] || input2 is object[,] || input3 is object[,];
            // 三参版本同款形状预校验。
            if (was2D && HasMismatched2DShapes(input1!, input2!, input3!))
                return ExcelError.Value;

            // 同上：三参版本空输入 → null。
            if (flat1.Length == 0 || flat2.Length == 0 || flat3.Length == 0)
                return null!;

            int targetLen = Math.Max(Math.Max(flat1.Length, flat2.Length), flat3.Length);

            if (targetLen == 1)
                return was2D
                    ? new object[,] { { MapSingleCell(flat1[0], flat2[0], flat3[0], mapper) } }
                    : MapSingleCell(flat1[0], flat2[0], flat3[0], mapper);

            if ((flat1.Length != 1 && flat1.Length != targetLen) ||
                (flat2.Length != 1 && flat2.Length != targetLen) ||
                (flat3.Length != 1 && flat3.Length != targetLen))
                return ExcelError.Value;

            var result = new object[targetLen];
            for (int i = 0; i < targetLen; i++)
            {
                object v1 = flat1.Length == 1 ? flat1[0] : flat1[i];
                object v2 = flat2.Length == 1 ? flat2[0] : flat2[i];
                object v3 = flat3.Length == 1 ? flat3[0] : flat3[i];
                result[i] = MapSingleCell(v1, v2, v3, mapper);
            }

            if (was2D)
                // was2D 门卫保证至少一个输入是 object[,]（非 null）；其余输入可为 null 标量，
                // ReshapeFlatToOriginal2D 对非 object[,] 元素安全跳过（`orig is object[,]`），
                // 故用 null-forgiving 仅消除 CS8604 告警。
                return ReshapeFlatToOriginal2D(result, input1!, input2!, input3!);

            return result;
        }

        // ─────────────────────────────────────────────────────────────────
        // Internal: single-cell mapping with Error/Null/Empty propagation
        // ─────────────────────────────────────────────────────────────────

        private static object MapSingleCell<TInput, TOutput>(
            object cell, Func<TInput, TOutput> mapper)
        {
            if (cell == null) return cell!;
            if (cell is DBNull) return null!;
            // ExcelMissing（公式栏省略的自变量）须映射为省略 → null：落 ConvertValue 后
            // TInput=object 时原样传给 mapper，类型全名泄漏进结果
            // （STR.FORMAT(,"0.00") → "ExcelDna.Integration.ExcelMissing"）。
            if (InputNormalizer.IsExcelMissing(cell)) return null!;
            // return the ORIGINAL empty sentinel — Excel-DNA renders its own ExcelEmpty as an
            // empty cell, while Foundation.ExcelEmpty (a custom class outside the Excel-DNA
            // marshalling allow-list) would render as #NUM! in real Excel.
            if (InputNormalizer.IsExcelEmptyValue(cell)) return cell!;
            if (InputNormalizer.IsExcelErrorValue(cell)) return cell;

            return MapValue(cell, mapper);
        }

        private static object MapSingleCell<T1, T2, TOutput>(
            object cell1, object cell2, Func<T1, T2, TOutput> mapper)
        {
            if (InputNormalizer.IsExcelErrorValue(cell1)) return cell1;
            if (InputNormalizer.IsExcelErrorValue(cell2)) return cell2;
            if (cell1 == null) return cell1!;
            if (cell2 == null) return cell2!;
            if (cell1 is DBNull) return null!;
            if (cell2 is DBNull) return null!;
            // ExcelMissing → 省略 → null（同单参版本）。
            if (InputNormalizer.IsExcelMissing(cell1) || InputNormalizer.IsExcelMissing(cell2)) return null!;
            // pass through the original empty sentinel (Excel-DNA renders as empty).
            if (InputNormalizer.IsExcelEmptyValue(cell1)) return cell1!;
            if (InputNormalizer.IsExcelEmptyValue(cell2)) return cell2!;

            return MapValue(cell1, cell2, mapper);
        }

        private static object MapSingleCell<T1, T2, T3, TOutput>(
            object cell1, object cell2, object cell3,
            Func<T1, T2, T3, TOutput> mapper)
        {
            if (InputNormalizer.IsExcelErrorValue(cell1)) return cell1;
            if (InputNormalizer.IsExcelErrorValue(cell2)) return cell2;
            if (InputNormalizer.IsExcelErrorValue(cell3)) return cell3;
            if (cell1 == null) return cell1!;
            if (cell2 == null) return cell2!;
            if (cell3 == null) return cell3!;
            if (cell1 is DBNull) return null!;
            if (cell2 is DBNull) return null!;
            if (cell3 is DBNull) return null!;
            // ExcelMissing → 省略 → null（同单参版本）。
            if (InputNormalizer.IsExcelMissing(cell1) || InputNormalizer.IsExcelMissing(cell2)
                || InputNormalizer.IsExcelMissing(cell3)) return null!;
            // pass through the original empty sentinel (Excel-DNA renders as empty).
            if (InputNormalizer.IsExcelEmptyValue(cell1)) return cell1!;
            if (InputNormalizer.IsExcelEmptyValue(cell2)) return cell2!;
            if (InputNormalizer.IsExcelEmptyValue(cell3)) return cell3!;

            return MapValue(cell1, cell2, cell3, mapper);
        }

        // ── Type coercion + mapping ──────────────────────────────────────

        private static object MapValue<TInput, TOutput>(
            object value, Func<TInput, TOutput> mapper)
        {
            // ConvertValue 必须在 try 内：它调用 InputNormalizer.ToInt32/ToLong
            // （对越界值主动抛 ArgumentException），任一单元格触发即冲出 Map2D 双层循环
            // → 整片区域一个 #VALUE!，per-cell 隔离承诺失效。单格转换失败返回 ExcelError.Value。
            try
            {
                TInput typed = ConvertValue<TInput>(value);
                TOutput result = mapper(typed);
                return (object)result!;
            }
            catch (Exception ex) when (ExceptionFilters.IsCatchable(ex))
            {
                // Per-cell isolation: a failing cell returns #VALUE! instead of
                // aborting the entire array. Critical for DateTime UDFs where
                // D() or AssertValidDate throws on empty/error cells in a range.
                System.Diagnostics.Debug.WriteLine(
                    $"[MapValue] Cell mapper failed for '{typeof(TInput).Name}'->'{typeof(TOutput).Name}': {ex.Message}");
                return ExcelError.Value;
            }
        }

        private static object MapValue<T1, T2, TOutput>(
            object v1, object v2, Func<T1, T2, TOutput> mapper)
        {
            try
            {
                T1 t1 = ConvertValue<T1>(v1);
                T2 t2 = ConvertValue<T2>(v2);
                TOutput result = mapper(t1, t2);
                return (object)result!;
            }
            catch (Exception ex) when (ExceptionFilters.IsCatchable(ex))
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[MapValue] Cell mapper failed for '<{typeof(T1).Name},{typeof(T2).Name}>'->'{typeof(TOutput).Name}': {ex.Message}");
                return ExcelError.Value;
            }
        }

        private static object MapValue<T1, T2, T3, TOutput>(
            object v1, object v2, object v3, Func<T1, T2, T3, TOutput> mapper)
        {
            try
            {
                T1 t1 = ConvertValue<T1>(v1);
                T2 t2 = ConvertValue<T2>(v2);
                T3 t3 = ConvertValue<T3>(v3);
                TOutput result = mapper(t1, t2, t3);
                return (object)result!;
            }
            catch (Exception ex) when (ExceptionFilters.IsCatchable(ex))
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[MapValue] Cell mapper failed for '<{typeof(T1).Name},{typeof(T2).Name},{typeof(T3).Name}>'->'{typeof(TOutput).Name}': {ex.Message}");
                return ExcelError.Value;
            }
        }

        private static T ConvertValue<T>(object value)
        {
            if (value is T typed) return typed;

            Type targetType = typeof(T);
            if (targetType == typeof(string)) return (T)(object)InputNormalizer.ToString(value);
            if (targetType == typeof(double)) return (T)(object)InputNormalizer.ToDouble(value);
            if (targetType == typeof(long)) return (T)(object)InputNormalizer.ToLong(value);
            // 统一委托 InputNormalizer.ToInt32：CLAMP（截断后依赖下游范围检查兜底）与
            // ToInt32 的 THROW 语义不一致（超 int 范围应显式失败而非静默钳制）。
            if (targetType == typeof(int)) return (T)(object)InputNormalizer.ToInt32(value);
            if (targetType == typeof(bool)) return (T)(object)InputNormalizer.ToBool(value);
            if (targetType == typeof(DateTime)) return (T)(object)InputNormalizer.ToDateTime(value);

            try
            {
                return (T)Convert.ChangeType(value, targetType,
                    System.Globalization.CultureInfo.InvariantCulture);
            }
            catch (Exception ex) when (ExceptionFilters.IsCatchable(ex))
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[ConvertValue] Failed to convert '{value?.GetType().Name}' to '{typeof(T).Name}': {ex.Message}");
                throw; // re-throw for all types: WrapError → #VALUE!
            }
        }

        // ── Array helpers ─────────────────────────────────────────────────

        private static object[] Map1D<TInput, TOutput>(
            object[] arr, Func<TInput, TOutput> mapper)
        {
            var result = new object[arr.Length];
            for (int i = 0; i < arr.Length; i++)
                result[i] = MapSingleCell(arr[i], mapper);
            return result;
        }

        private static object[,] Map2D<TInput, TOutput>(
            object[,] arr, Func<TInput, TOutput> mapper)
        {
            int rows = arr.GetLength(0);
            int cols = arr.GetLength(1);
            var result = new object[rows, cols];
            for (int r = 0; r < rows; r++)
                for (int c = 0; c < cols; c++)
                    result[r, c] = MapSingleCell(arr[r, c], mapper);
            return result;
        }

        private static object MapMultiBroadcast<T1, T2, TOutput>(
            object scalar, object[] arr, Func<T1, T2, TOutput> mapper,
            bool was2D, object orig1, object orig2)
        {
            var result = new object[arr.Length];
            for (int i = 0; i < arr.Length; i++)
                result[i] = MapSingleCell(scalar, arr[i], mapper);
            if (was2D) return ReshapeFlatToOriginal2D(result, orig1!, orig2!);
            return result;
        }

        private static object PreserveShape2D<T1, T2, TOutput>(
            object[] arr, object scalar, Func<T1, T2, TOutput> mapper,
            bool was2D, object orig1, object orig2)
        {
            var result = new object[arr.Length];
            for (int i = 0; i < arr.Length; i++)
                result[i] = MapSingleCell(arr[i], scalar, mapper);
            if (was2D) return ReshapeFlatToOriginal2D(result, orig1!, orig2!);
            return result;
        }

        private static object MapMultiSameLength<T1, T2, TOutput>(
            object[] flat1, object[] flat2, Func<T1, T2, TOutput> mapper,
            bool was2D, object orig1, object orig2)
        {
            var result = new object[flat1.Length];
            for (int i = 0; i < flat1.Length; i++)
                result[i] = MapSingleCell(flat1[i], flat2[i], mapper);
            if (was2D) return ReshapeFlatToOriginal2D(result, orig1!, orig2!);
            return result;
        }

        /// <summary>True when two or more 2D inputs (excluding 1×1 scalar-semantics cells)
        /// have different shapes — MapOverMulti's documented contract is ExcelError.Value
        /// for mismatched sizes, so this is checked before mapping.</summary>
        private static bool HasMismatched2DShapes(params object[] inputs)
        {
            (int Rows, int Cols)? shape = null;
            foreach (object input in inputs)
            {
                if (input is object[,] a)
                {
                    if (a.GetLength(0) == 1 && a.GetLength(1) == 1) continue;
                    var s = (a.GetLength(0), a.GetLength(1));
                    if (shape == null) shape = s;
                    else if (shape.Value != s) return true;
                }
            }
            return false;
        }

        private static object[,] ReshapeFlatToOriginal2D(
            object[] flat, params object[] originals)
        {
            int rows = flat.Length;
            // Validate all 2D inputs share the same row count — otherwise
            // same-length arrays with different shapes (e.g. [6,1] and [2,3])
            // would silently produce a wrong output shape.
            int? expectedRows = null;
            foreach (var orig in originals)
            {
                if (orig is object[,] arr2D)
                {
                    int r = arr2D.GetLength(0);
                    // 1×1 输入是标量语义（Excel 单元格即 1×1 range），不参与行数一致性校验——
                    // MapOverMulti(1×1, n×1) 必须广播为 n×1，而非抛 "Cannot reshape"。
                    if (r == 1 && arr2D.GetLength(1) == 1) continue;
                    if (expectedRows == null) { expectedRows = r; rows = r; }
                    else if (expectedRows.Value != r)
                        throw new InvalidOperationException(
                            $"Cannot reshape into consistent 2D shape: one input has {expectedRows.Value} " +
                            $"rows but another has {r} rows. Use identically-shaped ranges.");
                }
            }
            // If flat doesn't divide evenly into rows, the input shapes are inconsistent.
            if (rows == 0 || flat.Length % rows != 0)
                throw new InvalidOperationException(
                    $"Cannot reshape {flat.Length} elements into {rows} rows — " +
                    "the input arrays have mismatched dimensions.");
            int cols = flat.Length / rows;
            if (cols == 0) cols = 1;

            var result = new object[rows, cols];
            for (int i = 0; i < flat.Length && i < rows * cols; i++)
                result[i / cols, i % cols] = flat[i];
            return result;
        }
    }
}
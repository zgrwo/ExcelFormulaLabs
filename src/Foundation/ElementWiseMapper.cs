using System;

namespace FormulaLabs.Foundation
{
    /// <summary>
    /// Element-wise mapping over scalar or array inputs — the core abstraction
    /// that eliminates ~3000 lines of duplicated boilerplate across 219 UDF wrappers.
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

            // was2D is checked AFTER TryExtractComRangeValue (line 92-93) because
            // COM Range extraction converts the input to its .Value (object[,] for
            // multi-cell ranges), so the post-extraction check correctly detects
            // both native object[,] and COM-Range-originated 2D arrays.
            bool was2D = input1 is object[,] || input2 is object[,];

            if (flat1.Length == 0 || flat2.Length == 0)
                return ExcelEmpty.Value;

            if (flat1.Length == 1 && flat2.Length == 1)
                return was2D
                    ? new object[,] { { MapSingleCell(flat1[0], flat2[0], mapper) } }
                    : MapSingleCell(flat1[0], flat2[0], mapper);

            if (flat1.Length == 1)
                return MapMultiBroadcast(flat1[0], flat2, mapper, was2D, input1, input2);

            if (flat2.Length == 1)
                return PreserveShape2D(flat1, flat2[0], mapper, was2D, input1, input2);

            if (flat1.Length == flat2.Length)
                return MapMultiSameLength(flat1, flat2, mapper, was2D, input1, input2);

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

            bool was2D = input1 is object[,] || input2 is object[,] || input3 is object[,];

            if (flat1.Length == 0 || flat2.Length == 0 || flat3.Length == 0)
                return ExcelEmpty.Value;

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
                return ReshapeFlatToOriginal2D(result, input1, input2, input3);

            return result;
        }

        // ─────────────────────────────────────────────────────────────────
        // Internal: single-cell mapping with Error/Null/Empty propagation
        // ─────────────────────────────────────────────────────────────────

        private static object MapSingleCell<TInput, TOutput>(
            object cell, Func<TInput, TOutput> mapper)
        {
            if (cell == null) return cell!;
            if (cell is DBNull) return ExcelEmpty.Value;
            if (InputNormalizer.IsExcelEmptyValue(cell)) return ExcelEmpty.Value;
            if (cell is ExcelError) return cell;

            return MapValue(cell, mapper);
        }

        private static object MapSingleCell<T1, T2, TOutput>(
            object cell1, object cell2, Func<T1, T2, TOutput> mapper)
        {
            if (cell1 is ExcelError) return cell1;
            if (cell2 is ExcelError) return cell2;
            if (cell1 == null) return cell1!;
            if (cell2 == null) return cell2!;
            if (cell1 is DBNull) return ExcelEmpty.Value;
            if (cell2 is DBNull) return ExcelEmpty.Value;
            if (InputNormalizer.IsExcelEmptyValue(cell1)) return ExcelEmpty.Value;
            if (InputNormalizer.IsExcelEmptyValue(cell2)) return ExcelEmpty.Value;

            return MapValue(cell1, cell2, mapper);
        }

        private static object MapSingleCell<T1, T2, T3, TOutput>(
            object cell1, object cell2, object cell3,
            Func<T1, T2, T3, TOutput> mapper)
        {
            if (cell1 is ExcelError) return cell1;
            if (cell2 is ExcelError) return cell2;
            if (cell3 is ExcelError) return cell3;
            if (cell1 == null) return cell1!;
            if (cell2 == null) return cell2!;
            if (cell3 == null) return cell3!;
            if (cell1 is DBNull) return ExcelEmpty.Value;
            if (cell2 is DBNull) return ExcelEmpty.Value;
            if (cell3 is DBNull) return ExcelEmpty.Value;
            if (InputNormalizer.IsExcelEmptyValue(cell1)) return ExcelEmpty.Value;
            if (InputNormalizer.IsExcelEmptyValue(cell2)) return ExcelEmpty.Value;
            if (InputNormalizer.IsExcelEmptyValue(cell3)) return ExcelEmpty.Value;

            return MapValue(cell1, cell2, cell3, mapper);
        }

        // ── Type coercion + mapping ──────────────────────────────────────

        private static object MapValue<TInput, TOutput>(
            object value, Func<TInput, TOutput> mapper)
        {
            TInput typed = ConvertValue<TInput>(value);
            try
            {
                TOutput result = mapper(typed);
                return (object)result!;
            }
            catch (Exception ex) when (ex is not OutOfMemoryException
                and not StackOverflowException
                and not AccessViolationException)
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
            T1 t1 = ConvertValue<T1>(v1);
            T2 t2 = ConvertValue<T2>(v2);
            try
            {
                TOutput result = mapper(t1, t2);
                return (object)result!;
            }
            catch (Exception ex) when (ex is not OutOfMemoryException
                and not StackOverflowException
                and not AccessViolationException)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[MapValue] Cell mapper failed for '<{typeof(T1).Name},{typeof(T2).Name}>'->'{typeof(TOutput).Name}': {ex.Message}");
                return ExcelError.Value;
            }
        }

        private static object MapValue<T1, T2, T3, TOutput>(
            object v1, object v2, object v3, Func<T1, T2, T3, TOutput> mapper)
        {
            T1 t1 = ConvertValue<T1>(v1);
            T2 t2 = ConvertValue<T2>(v2);
            T3 t3 = ConvertValue<T3>(v3);
            try
            {
                TOutput result = mapper(t1, t2, t3);
                return (object)result!;
            }
            catch (Exception ex) when (ex is not OutOfMemoryException
                and not StackOverflowException
                and not AccessViolationException)
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
            if (targetType == typeof(int)) return (T)(object)(int)InputNormalizer.ToLong(value);
            if (targetType == typeof(bool)) return (T)(object)InputNormalizer.ToBool(value);
            if (targetType == typeof(DateTime)) return (T)(object)InputNormalizer.ToDateTime(value);

            try
            {
                return (T)Convert.ChangeType(value, targetType,
                    System.Globalization.CultureInfo.InvariantCulture);
            }
            catch (Exception ex) when (ex is not OutOfMemoryException
                and not StackOverflowException
                and not AccessViolationException)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[ConvertValue] Failed to convert '{value?.GetType().Name}' to '{typeof(T).Name}': {ex.Message}");
                if (typeof(T) == typeof(double)) return (T)(object)double.NaN;
                throw; // re-throw for non-double types: WrapError → #VALUE!
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
            if (was2D) return ReshapeFlatToOriginal2D(result, orig1, orig2);
            return result;
        }

        private static object PreserveShape2D<T1, T2, TOutput>(
            object[] arr, object scalar, Func<T1, T2, TOutput> mapper,
            bool was2D, object orig1, object orig2)
        {
            var result = new object[arr.Length];
            for (int i = 0; i < arr.Length; i++)
                result[i] = MapSingleCell(arr[i], scalar, mapper);
            if (was2D) return ReshapeFlatToOriginal2D(result, orig1, orig2);
            return result;
        }

        private static object MapMultiSameLength<T1, T2, TOutput>(
            object[] flat1, object[] flat2, Func<T1, T2, TOutput> mapper,
            bool was2D, object orig1, object orig2)
        {
            var result = new object[flat1.Length];
            for (int i = 0; i < flat1.Length; i++)
                result[i] = MapSingleCell(flat1[i], flat2[i], mapper);
            if (was2D) return ReshapeFlatToOriginal2D(result, orig1, orig2);
            return result;
        }

        private static object[,] ReshapeFlatToOriginal2D(
            object[] flat, params object[] originals)
        {
            int rows = flat.Length;
            foreach (var orig in originals)
            {
                if (orig is object[,] arr2D)
                {
                    rows = arr2D.GetLength(0);
                    break;
                }
            }
            // If flat doesn't divide evenly into rows, return as single-row to preserve all values.
            if (rows == 0 || flat.Length % rows != 0) rows = 1;
            int cols = flat.Length / rows;
            if (cols == 0) cols = 1;

            var result = new object[rows, cols];
            for (int i = 0; i < flat.Length && i < rows * cols; i++)
                result[i / cols, i % cols] = flat[i];
            return result;
        }
    }
}

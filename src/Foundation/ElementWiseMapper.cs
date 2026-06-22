using System;

namespace ExcelVbaLibraries.Foundation
{
    /// <summary>
    /// Element-wise mapping over scalar or array inputs — the core abstraction
    /// that eliminates ~3000 lines of duplicated boilerplate across 233 UDF wrappers.
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

            bool was2D = input1 is object[,] || input2 is object[,];

            if (flat1.Length == 0 || flat2.Length == 0)
                return ExcelEmpty.Value;

            if (flat1.Length == 1 && flat2.Length == 1)
                return MapSingleCell(flat1[0], flat2[0], mapper);

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
                return MapSingleCell(flat1[0], flat2[0], flat3[0], mapper);

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
            if (cell == null || cell is DBNull) return cell!;
            if (ReferenceEquals(cell, ExcelEmpty.Value)) return ExcelEmpty.Value;
            if (cell is ExcelError) return cell;

            return MapValue(cell, mapper);
        }

        private static object MapSingleCell<T1, T2, TOutput>(
            object cell1, object cell2, Func<T1, T2, TOutput> mapper)
        {
            if (cell1 is ExcelError) return cell1;
            if (cell2 is ExcelError) return cell2;
            if (cell1 == null || cell1 is DBNull) return cell1!;
            if (cell2 == null || cell2 is DBNull) return cell2!;
            if (ReferenceEquals(cell1, ExcelEmpty.Value)) return ExcelEmpty.Value;
            if (ReferenceEquals(cell2, ExcelEmpty.Value)) return ExcelEmpty.Value;

            T1 t1 = ConvertValue<T1>(cell1);
            T2 t2 = ConvertValue<T2>(cell2);
            return (object)mapper(t1, t2)!;
        }

        private static object MapSingleCell<T1, T2, T3, TOutput>(
            object cell1, object cell2, object cell3,
            Func<T1, T2, T3, TOutput> mapper)
        {
            if (cell1 is ExcelError) return cell1;
            if (cell2 is ExcelError) return cell2;
            if (cell3 is ExcelError) return cell3;
            if (cell1 == null || cell1 is DBNull) return cell1!;
            if (cell2 == null || cell2 is DBNull) return cell2!;
            if (cell3 == null || cell3 is DBNull) return cell3!;
            if (ReferenceEquals(cell1, ExcelEmpty.Value)) return ExcelEmpty.Value;
            if (ReferenceEquals(cell2, ExcelEmpty.Value)) return ExcelEmpty.Value;
            if (ReferenceEquals(cell3, ExcelEmpty.Value)) return ExcelEmpty.Value;

            return MapValue(cell1, cell2, cell3, mapper)!;
        }

        // ── Type coercion + mapping ──────────────────────────────────────

        private static object MapValue<TInput, TOutput>(
            object value, Func<TInput, TOutput> mapper)
        {
            TInput typed = ConvertValue<TInput>(value);
            TOutput result = mapper(typed);
            return (object)result!;
        }

        private static TOutput MapValue<T1, T2, TOutput>(
            object v1, object v2, Func<T1, T2, TOutput> mapper)
        {
            T1 t1 = ConvertValue<T1>(v1);
            T2 t2 = ConvertValue<T2>(v2);
            return mapper(t1, t2);
        }

        private static TOutput MapValue<T1, T2, T3, TOutput>(
            object v1, object v2, object v3, Func<T1, T2, T3, TOutput> mapper)
        {
            T1 t1 = ConvertValue<T1>(v1);
            T2 t2 = ConvertValue<T2>(v2);
            T3 t3 = ConvertValue<T3>(v3);
            return mapper(t1, t2, t3);
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
            catch { return default!; }
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
            // If flat doesn't divide evenly into rows, don't silently truncate.
            // Return as single-row to preserve all values.
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

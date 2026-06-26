using System;
using System.Globalization;
using System.Runtime.InteropServices;

namespace ExcelVbaLibraries.Foundation
{
    /// <summary>
    /// Flattening direction for 2D→1D conversion.
    /// </summary>
    public enum NormalizeOrder
    {
        /// <summary>Row-major: row 0 col 0, row 0 col 1, ..., row 1 col 0, ...</summary>
        RowMajor,

        /// <summary>Column-major: col 0 row 0, col 0 row 1, ..., col 1 row 0, ...</summary>
        ColumnMajor
    }

    /// <summary>
    /// Type probing, safe coercion, and array normalisation.
    /// Ported from VariantKit.cls: IsEmptyArray, ArrayDims, Is1D, Is2D,
    /// IsNumericCell, Normalize1D, NormalizeTo2D, NormalizeInput, Normalize2D,
    /// ToDoubles, WrapScalar.
    /// </summary>
    /// <remarks>
    /// This class bridges the gap between Excel's loosely-typed <c>object</c> world
    /// (Range references, mixed-type arrays, Error/Empty markers) and .NET's
    /// strongly-typed generics. It is used by <see cref="ElementWiseMapper"/> and
    /// by every UDF wrapper that needs type coercion.
    ///
    /// Foundation.dll has zero NuGet dependencies, so COM Range detection uses
    /// <see cref="Marshal.IsComObject"/> + <c>dynamic</c> dispatch rather than a
    /// strongly-typed Excel Interop reference.
    /// </remarks>
    public static class InputNormalizer
    {
        // ─────────────────────────────────────────────────────────────────
        // COM Range extraction
        // ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// If <paramref name="input"/> is a COM Range object, extract its
        /// <c>.Value</c> (or <c>.Areas(1).Value</c> for multi-area ranges)
        /// into <paramref name="value"/>. Returns <c>true</c> if extraction occurred.
        /// </summary>
        /// <remarks>
        /// Detection strategy (mirrors VBA's <c>TypeOf x Is Range</c>):
        /// 1. Fast path: if not a COM object, return false immediately.
        /// 2. Probe the <c>.Areas</c> property — unique to Excel Range objects.
        /// 3. If <c>.Areas</c> exists, read <c>.Value</c> (or first area's value).
        ///
        /// This method is internal because UDF wrappers should use
        /// <see cref="ElementWiseMapper"/> which calls this automatically.
        /// Direct callers in Excel-DNA host assemblies can bypass this with
        /// <c>ExcelReference.GetValue()</c> for better performance.
        /// </remarks>
        internal static bool TryExtractComRangeValue(object input, out object value)
        {
            value = input;

            if (input == null) return false;
            if (!Marshal.IsComObject(input)) return false;

            try
            {
                dynamic dyn = input;

                // Probe: does it have .Areas? (unique to Range, not Worksheet/Application/etc.)
                try
                {
                    dynamic areas = dyn.Areas;
                    // If we get here, it's very likely a Range
                }
                catch (Exception ex) when (ex is not OutOfMemoryException
                    and not StackOverflowException and not AccessViolationException)
                {
                    // No .Areas property → not a Range
                    return false;
                }

                // Extract .Value
                try
                {
                    dynamic raw = dyn.Value;
                    value = raw;

                    // Multi-area Range: take only Areas(1)
                    try
                    {
                        if (dyn.Areas.Count > 1)
                            value = dyn.Areas[1].Value;
                    }
                    catch (Exception ex) when (ex is not OutOfMemoryException
                        and not StackOverflowException and not AccessViolationException)
                    {
                        // Single area or Areas not enumerable — fine, use .Value
                    }

                    return true;
                }
                catch (Exception ex) when (ex is not OutOfMemoryException
                    and not StackOverflowException and not AccessViolationException)
                {
                    return false;
                }
            }
            catch (Exception ex) when (ex is not OutOfMemoryException
                and not StackOverflowException and not AccessViolationException)
            {
                return false;
            }
        }

        // ─────────────────────────────────────────────────────────────────
        // Probe methods
        // ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// Returns <c>true</c> if the value is a zero-length array.
        /// Equivalent to VBA: <c>UBound(v) &lt; LBound(v)</c>.
        /// </summary>
        public static bool IsEmptyArray(object? value)
        {
            if (value == null) return false;
            if (value is Array arr)
                return arr.Length == 0;
            return false;
        }

        /// <summary>
        /// Returns the number of dimensions: 0 = scalar, 1 = 1D, 2 = 2D.
        /// </summary>
        public static int ArrayDims(object? value)
        {
            if (value == null) return 0;
            if (value is object[,]) return 2;
            if (value is Array arr) return arr.Rank;
            return 0;
        }

        /// <summary>True if the value is a 1D array.</summary>
        public static bool Is1D(object? value) => ArrayDims(value) == 1;

        /// <summary>True if the value is a 2D array.</summary>
        public static bool Is2D(object? value) => ArrayDims(value) == 2;

        /// <summary>
        /// Returns <c>true</c> if the value is a numeric cell.
        /// Matches VBA VariantKit.IsNumericCell: rejects Empty, Boolean, Date, Error,
        /// and empty/whitespace strings. Accepts numeric types and numeric-looking strings.
        /// </summary>
        /// <remarks>
        /// This is the most restrictive of the three IsNumeric variants in the codebase.
        /// It explicitly rejects <c>bool</c> and <c>DateTime</c> because VBA treats them as
        /// distinct non-numeric subtypes for cell-type probing. For sort/comparison purposes
        /// where bool→1.0 and DateTime→OLE Date are acceptable, see
        /// <see cref="ComparisonUtils.IsNumeric"/> (internal, shared with
        /// <see cref="ArrayOperations"/>).
        /// </remarks>
        public static bool IsNumericCell(object? value)
        {
            if (value == null) return false;
            if (value is DBNull) return false;
            if (ReferenceEquals(value, ExcelEmpty.Value)) return false;
            if (value is bool) return false;         // VBA: Boolean is NOT numeric for cell purposes
            if (value is DateTime) return false;     // VBA: Date is NOT numeric for cell purposes
            if (value is ExcelError) return false;

            if (value is int || value is long || value is float || value is double
                || value is decimal || value is short || value is byte
                || value is sbyte || value is ushort || value is uint || value is ulong)
                return true;

            if (value is string s)
            {
                if (string.IsNullOrWhiteSpace(s)) return false;
                // NumberStyles.Float includes AllowLeadingWhite|AllowTrailingWhite,
                // so Trim() is unnecessary — TryParse skips whitespace on its own.
                return double.TryParse(s, NumberStyles.Float | NumberStyles.AllowThousands,
                    CultureInfo.InvariantCulture, out _);
            }

            return false;
        }

        // ─────────────────────────────────────────────────────────────────
        // Safe type coercion
        // ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// Detect Excel-DNA's <c>ExcelMissing.Value</c> sentinel without a hard
        /// assembly reference. When a user omits an optional UDF argument in the
        /// formula bar, Excel-DNA passes this sentinel; treating it as missing
        /// prevents garbage like the type name leaking into computation results.
        /// </summary>
        internal static bool IsExcelMissing(object? value)
        {
            return value != null
                && value.GetType().FullName == "ExcelDna.Integration.ExcelMissing";
        }

        /// <summary>
        /// Safe conversion to string. Error/Null/Empty → "".
        /// </summary>
        public static string ToString(object? value)
        {
            if (value == null || value is DBNull || IsExcelMissing(value)) return "";
            if (ReferenceEquals(value, ExcelEmpty.Value)) return "";
            if (value is ExcelError) return "";
            if (value is string s) return s;
            return Convert.ToString(value, CultureInfo.InvariantCulture) ?? "";
        }

        /// <summary>
        /// Safe conversion to double. Error/Null/Empty → NaN.
        /// Non-numeric strings → NaN.
        /// </summary>
        public static double ToDouble(object? value)
        {
            if (value == null || value is DBNull || IsExcelMissing(value)) return double.NaN;
            if (ReferenceEquals(value, ExcelEmpty.Value)) return double.NaN;
            if (value is ExcelError) return double.NaN;
            if (value is double d) return (double.IsNaN(d) || double.IsInfinity(d)) ? double.NaN : d; // L1 NaN/Inf guard
            if (value is int i) return i;
            if (value is long l) return l;
            if (value is float f) return f;
            if (value is decimal m) { double dm = (double)m; return double.IsInfinity(dm) ? double.NaN : dm; }
            if (value is string s)
            {
                if (double.TryParse(s, NumberStyles.Float | NumberStyles.AllowThousands,
                    CultureInfo.InvariantCulture, out double result))
                    return result;
                return double.NaN;
            }
            try { return Convert.ToDouble(value, CultureInfo.InvariantCulture); }
            catch (Exception ex) when (ex is not OutOfMemoryException
                and not StackOverflowException
                and not AccessViolationException)
            { return double.NaN; }
        }

        /// <summary>
        /// Safe conversion to long. Error/Null/Empty → 0.
        /// </summary>
        public static long ToLong(object? value)
        {
            if (value == null || value is DBNull || IsExcelMissing(value)) return 0;
            if (ReferenceEquals(value, ExcelEmpty.Value)) return 0;
            if (value is ExcelError) return 0;
            if (value is long l) return l;
            if (value is int i) return i;
            if (value is double d)
            {
                if (double.IsNaN(d) || double.IsInfinity(d)) return 0; // L1 NaN/Inf guard
                return (long)Math.Round(d);
            }
            if (value is string s)
            {
                if (long.TryParse(s, NumberStyles.Integer | NumberStyles.AllowThousands,
                    CultureInfo.InvariantCulture, out long result))
                    return result;
                if (double.TryParse(s, NumberStyles.Float | NumberStyles.AllowThousands,
                    CultureInfo.InvariantCulture, out double dVal))
                {
                    if (double.IsNaN(dVal) || double.IsInfinity(dVal)) return 0; // L1 NaN/Inf guard
                    return (long)Math.Round(dVal);
                }
                return 0;
            }
            try { return Convert.ToInt64(value, CultureInfo.InvariantCulture); }
            catch (Exception ex) when (ex is not OutOfMemoryException
                and not StackOverflowException
                and not AccessViolationException)
            { return 0; }
        }

        /// <summary>
        /// Safe conversion to bool. Error/Null/Empty → false.
        /// Numeric: 0 → false, non-zero → true. String: "true"/"1" → true.
        /// </summary>
        public static bool ToBool(object? value)
        {
            if (value == null || value is DBNull || IsExcelMissing(value)) return false;
            if (ReferenceEquals(value, ExcelEmpty.Value)) return false;
            if (value is ExcelError) return false;
            if (value is bool b) return b;
            if (value is double d) return double.IsNaN(d) ? false : d != 0.0; // L1 NaN guard
            if (value is int i) return i != 0;
            if (value is long l) return l != 0;
            if (value is string s)
            {
                s = s.Trim();
                if (s.Length == 0) return false;
                if (bool.TryParse(s, out bool bResult)) return bResult;
                if (s == "1") return true;
                if (s == "0") return false;
                if (double.TryParse(s, out double dVal)) return dVal != 0.0;
                return false;
            }
            try { return Convert.ToBoolean(value, CultureInfo.InvariantCulture); }
            catch (Exception ex) when (ex is not OutOfMemoryException
                and not StackOverflowException
                and not AccessViolationException)
            { return false; }
        }

        /// <summary>
        /// Safe conversion to DateTime. Error/Null/Empty → DateTime.MinValue.
        /// Numeric values are treated as Excel serial dates (1899-12-30 epoch).
        /// </summary>
        public static DateTime ToDateTime(object? value)
        {
            if (value == null || value is DBNull || IsExcelMissing(value)) return DateTime.MinValue;
            if (ReferenceEquals(value, ExcelEmpty.Value)) return DateTime.MinValue;
            if (value is ExcelError) return DateTime.MinValue;
            if (value is DateTime dt) return dt;
            if (value is double d && d > 0 && !double.IsNaN(d) && !double.IsInfinity(d))
            {
                try { return new DateTime(1899, 12, 30).AddDays(d); }
                catch (Exception ex) when (ex is not OutOfMemoryException
                    and not StackOverflowException
                    and not AccessViolationException)
                { return DateTime.MinValue; }
            }
            if (value is string s)
            {
                if (DateTime.TryParse(s, CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out DateTime result))
                    return result;
                return DateTime.MinValue;
            }
            try { return Convert.ToDateTime(value, CultureInfo.InvariantCulture); }
            catch (Exception ex) when (ex is not OutOfMemoryException
                and not StackOverflowException
                and not AccessViolationException)
            { return DateTime.MinValue; }
        }

        // ─────────────────────────────────────────────────────────────────
        // Array normalisation
        // ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// Normalise any input to a 0-based 1D object array.
        /// </summary>
        /// <param name="input">Scalar, Range, 1D array, or 2D array.</param>
        /// <param name="order">Flattening direction for 2D→1D.</param>
        /// <returns>0-based 1D array, or <c>Array.Empty&lt;object&gt;()</c> for null.</returns>
        public static object[] NormalizeTo1D(object? input,
            NormalizeOrder order = NormalizeOrder.RowMajor)
        {
            if (input == null)
                return Array.Empty<object>();

            // COM Range → extract value first
            TryExtractComRangeValue(input, out input);

            if (input is object[,] arr2D)
            {
                int rows = arr2D.GetLength(0);
                int cols = arr2D.GetLength(1);
                var result = new object[rows * cols];

                if (order == NormalizeOrder.RowMajor)
                {
                    for (int r = 0; r < rows; r++)
                        for (int c = 0; c < cols; c++)
                            result[r * cols + c] = arr2D[r, c];
                }
                else
                {
                    for (int c = 0; c < cols; c++)
                        for (int r = 0; r < rows; r++)
                            result[c * rows + r] = arr2D[r, c];
                }

                return result;
            }

            if (input is object[] arr1D)
                return arr1D;   // Already 1D — pass through

            if (input is Array typedArr && typedArr.Rank == 1)
            {
                // e.g. int[], double[], string[] → convert to object[]
                var result = new object[typedArr.Length];
                for (int i = 0; i < typedArr.Length; i++)
                    result[i] = typedArr.GetValue(i)!;
                return result;
            }

            // Scalar → wrap as single-element array
            return new object[] { input };
        }

        /// <summary>
        /// Normalise any input to a 2D object array suitable for
        /// writing back to a worksheet Range.
        /// </summary>
        /// <param name="input">Scalar, Range, 1D array, or 2D array.</param>
        /// <returns>
        /// A object[,]. Scalar → [1,1]. 1D → [n,1] column vector.
        /// 2D → pass-through.
        /// </returns>
        public static object[,]? NormalizeTo2D(object? input)
        {
            if (input == null)
                return null;

            // COM Range → extract value first
            TryExtractComRangeValue(input, out input);

            if (input is object[,] arr2D)
                return arr2D;  // Already 2D

            if (input is object[] arr1D)
            {
                int n = arr1D.Length;
                var result = new object[n, 1];
                for (int i = 0; i < n; i++)
                    result[i, 0] = arr1D[i];
                return result;
            }

            if (input is Array typedArr && typedArr.Rank == 1)
            {
                int n = typedArr.Length;
                var result = new object[n, 1];
                for (int i = 0; i < n; i++)
                    result[i, 0] = typedArr.GetValue(i)!;
                return result;
            }

            if (input is Array multiDimArr && multiDimArr.Rank >= 2)
            {
                int rows = multiDimArr.GetLength(0);
                int cols = multiDimArr.GetLength(1);
                var result = new object[rows, cols];
                for (int r = 0; r < rows; r++)
                    for (int c = 0; c < cols; c++)
                        result[r, c] = multiDimArr.GetValue(r, c)!;
                return result;
            }

            // Scalar → 1×1
            return new object[,] { { input } };
        }

        /// <summary>
        /// Extract numeric values from mixed input into a double[].
        /// Non-numeric elements are skipped. Empty input returns empty double[].
        /// Matches VBA VariantKit.ToDoubles behaviour.
        /// </summary>
        public static double[] ToDoubles(object? input)
        {
            object[] flat = NormalizeTo1D(input);

            // Count numeric elements
            int count = 0;
            for (int i = 0; i < flat.Length; i++)
            {
                if (IsNumericCell(flat[i]))
                    count++;
            }

            if (count == 0)
                return Array.Empty<double>();

            // Extract — use the same predicate as the count loop, then
            // additionally filter NaN/Infinity from the result array.
            // IsNumericCell accepts "NaN"/"Infinity" strings (double.TryParse
            // returns true) but ToDouble returns NaN/Infinity for those.
            var result = new double[count];
            int idx = 0;
            for (int i = 0; i < flat.Length; i++)
            {
                if (IsNumericCell(flat[i]))
                {
                    double val = ToDouble(flat[i]);
                    if (!double.IsNaN(val) && !double.IsInfinity(val))
                    {
                        result[idx] = val;
                        idx++;
                    }
                }
            }
            // Trim excess slots if NaN/Infinity elements were filtered
            if (idx < count)
                Array.Resize(ref result, idx);

            return result;
        }
    }
}

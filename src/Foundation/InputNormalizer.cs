using System;
using System.Globalization;
using System.Runtime.InteropServices;

namespace ExcelFormulaLabs.Foundation
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
                catch (Exception ex) when (ExceptionFilters.IsCatchable(ex))
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
                    catch (Exception ex) when (ExceptionFilters.IsCatchable(ex))
                    {
                        // Single area or Areas not enumerable — fine, use .Value
                    }

                    return true;
                }
                catch (Exception ex) when (ExceptionFilters.IsCatchable(ex))
                {
                    return false;
                }
            }
            catch (Exception ex) when (ExceptionFilters.IsCatchable(ex))
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
            if (IsExcelEmptyValue(value)) return false;
            if (value is bool) return false;         // VBA: Boolean is NOT numeric for cell purposes
            if (value is DateTime) return false;     // VBA: Date is NOT numeric for cell purposes
            if (IsExcelErrorValue(value)) return false;

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

        // review 2026-09-05（R23）：Excel-DNA 哨兵类型全名由"魔法字符串"收敛为 const。
        // Foundation 不直接引用 ExcelDna.Integration（零依赖红线），只能按类型全名识别；
        // 字面量由 Foundation.Tests 的 InputNormalizerSentinelTypeNames 测试锁定——若 Excel-DNA
        // 升级改型名，测试会失败并提示同步。
        private const string ExcelDnaMissingTypeName = "ExcelDna.Integration.ExcelMissing";
        private const string ExcelDnaEmptyTypeName = "ExcelDna.Integration.ExcelEmpty";
        private const string ExcelDnaErrorTypeName = "ExcelDna.Integration.ExcelError";

        /// <summary>
        /// Detect Excel-DNA's <c>ExcelMissing.Value</c> sentinel without a hard
        /// assembly reference. When a user omits an optional UDF argument in the
        /// formula bar, Excel-DNA passes this sentinel; treating it as missing
        /// prevents garbage like the type name leaking into computation results.
        /// </summary>
        public static bool IsExcelMissing(object? value)
        {
            return value != null
                && value.GetType().FullName == ExcelDnaMissingTypeName;
        }

        /// <summary>
        /// Detect both <see cref="ExcelEmpty.Value"/> (Foundation) and
        /// <c>ExcelDna.Integration.ExcelEmpty</c> (Excel-DNA COM interop)
        /// without a hard assembly reference. The two types are distinct:
        /// Foundation's sentinel is used inside MapOver/ElementWiseMapper,
        /// while Excel-DNA's appears in empty cells from COM Range extraction.
        /// Treating both as "empty" prevents garbage text (type name via
        /// <c>.ToString()</c>) from leaking into computation results.
        /// </summary>
        public static bool IsExcelEmptyValue(object? value)
        {
            if (value == null) return false;
            return ReferenceEquals(value, ExcelEmpty.Value)
                || value.GetType().FullName == ExcelDnaEmptyTypeName;
        }

        /// <summary>
        /// Detect Excel error signals: Foundation <see cref="ExcelError"/> sentinel
        /// and <c>ExcelDna.Integration.ExcelError</c> (enum arriving from real Excel
        /// error cells) without a hard assembly reference.
        /// </summary>
        public static bool IsExcelErrorValue(object? value)
        {
            if (value == null) return false;
            return value is ExcelError
                || value.GetType().FullName == ExcelDnaErrorTypeName;
        }

        /// <summary>
        /// Safe conversion to string. Error/Null/Empty → "".
        /// </summary>
        public static string ToString(object? value)
        {
            if (value == null || value is DBNull || IsExcelMissing(value)) return "";
            if (IsExcelEmptyValue(value)) return "";
            if (IsExcelErrorValue(value)) return "";
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
            if (IsExcelEmptyValue(value)) return double.NaN;
            if (IsExcelErrorValue(value)) return double.NaN;
            if (value is double d) return (double.IsNaN(d) || double.IsInfinity(d)) ? double.NaN : d; // L1 NaN/Inf guard
            if (value is int i) return i;
            if (value is long l) return l;
            if (value is float f) return (float.IsNaN(f) || float.IsInfinity(f)) ? double.NaN : f;
            if (value is decimal m) { double dm = (double)m; return double.IsInfinity(dm) ? double.NaN : dm; }
            if (value is string s)
            {
                if (double.TryParse(s, NumberStyles.Float | NumberStyles.AllowThousands,
                    CultureInfo.InvariantCulture, out double result))
                    return (double.IsNaN(result) || double.IsInfinity(result)) ? double.NaN : result;
                return double.NaN;
            }
            try { return Convert.ToDouble(value, CultureInfo.InvariantCulture); }
            catch (Exception ex) when (ExceptionFilters.IsCatchable(ex))
            { return double.NaN; }
        }

        /// <summary>
        /// Safe conversion to long. Error/Null/Empty → 0.
        /// </summary>
        public static long ToLong(object? value)
        {
            if (value == null || value is DBNull || IsExcelMissing(value)) return 0;
            if (IsExcelEmptyValue(value)) return 0;
            if (IsExcelErrorValue(value)) return 0;
            if (value is long l) return l;
            if (value is int i) return i;
            if (value is double d)
            {
                if (double.IsNaN(d) || double.IsInfinity(d)) return 0; // L1 NaN/Inf guard
                // review 2026-09-05（R26 修正）：Math.Round 默认 ToEven（中点取偶）为有意行为——
                // 与移植源 VBA CLng 的银行家舍入一致（保真）。真实影响面 = 所有经 ToLong/ToInt32
                // 的半整数参数（如 STR.SUBSTITUTE 的 instance_num），与 Excel INT 截断的分歧仅限
                // 奇数半整数且多被后续 clamp 掩盖。
                double rd = Math.Round(d);
                // review 2026-08-31（深度审查 P2-8）：`rd > long.MaxValue` 有 2⁶³ 漏洞——
                // (double)long.MaxValue 不可精确表示，舍入为 2⁶³ = 9.223372036854776E18，
                // 于是 `2⁶³ > 2⁶³` 恒 false → 守卫绕过 → (long)rd 得 long.MinValue（net8 回绕/
                // net48 未定义，双 TFM 行为不一致）。用 2⁶³ 字面量严格小于比较。
                if (rd < long.MinValue || rd >= 9.223372036854776E18) return 0; // L2 range guard
                return (long)rd;
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
                    double rd = Math.Round(dVal);
                    // review 2026-08-31（深度审查 P2-8）：同 double 分支，2⁶³ 字面量严格比较。
                    if (rd < long.MinValue || rd >= 9.223372036854776E18) return 0; // L2 range guard
                    return (long)rd;
                }
                return 0;
            }
            try { return Convert.ToInt64(value, CultureInfo.InvariantCulture); }
            catch (Exception ex) when (ExceptionFilters.IsCatchable(ex))
            { return 0; }
        }

        /// <summary>
        /// Safe conversion to int. Error/Null/Empty → 0 (L4 sentinel).
        /// Values outside the int range throw instead of silently truncating —
        /// a raw <c>(int)</c> cast of a long beyond int.MaxValue wraps to a
        /// negative/incorrect index (review-2026-08-29 P2-2).
        /// </summary>
        public static int ToInt32(object? value)
        {
            long l = ToLong(value);
            if (l > int.MaxValue || l < int.MinValue)
                throw new ArgumentException(ErrorMsg.Get("Input_IntOutOfRange", l, int.MinValue, int.MaxValue));
            return (int)l;
        }

        /// <summary>
        /// Safe conversion to bool. Error/Null/Empty → false.
        /// Numeric: 0 → false, non-zero → true. String: "true"/"1" → true.
        /// </summary>
        public static bool ToBool(object? value)
        {
            if (value == null || value is DBNull || IsExcelMissing(value)) return false;
            if (IsExcelEmptyValue(value)) return false;
            if (IsExcelErrorValue(value)) return false;
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
                if (double.TryParse(s, NumberStyles.Float | NumberStyles.AllowThousands,
                    CultureInfo.InvariantCulture, out double dVal)) return dVal != 0.0;
                return false;
            }
            try { return Convert.ToBoolean(value, CultureInfo.InvariantCulture); }
            catch (Exception ex) when (ExceptionFilters.IsCatchable(ex))
            { return false; }
        }

        /// <summary>
        /// Safe conversion to bool with explicit sentinel value.
        /// <paramref name="sentinelValue"/> is returned only for Excel signal values
        /// (null, <see cref="DBNull"/>, <c>ExcelMissing</c>, empty, error).
        /// For all other values — including unparseable strings — the behaviour
        /// is identical to <see cref="ToBool(object?)"/> and ignores the sentinel.
        /// </summary>
        public static bool ToBool(object? value, bool sentinelValue)
        {
            if (value == null || value is DBNull || IsExcelMissing(value)) return sentinelValue;
            if (IsExcelEmptyValue(value)) return sentinelValue;
            if (IsExcelErrorValue(value)) return sentinelValue;
            return ToBool(value);
        }

        /// <summary>
        /// Safe conversion to DateTime. Error/Null/Empty → DateTime.MinValue.
        /// Numeric values are treated as Excel serial dates (1899-12-30 epoch).
        /// </summary>
        public static DateTime ToDateTime(object? value)
        {
            if (value == null || value is DBNull || IsExcelMissing(value)) return DateTime.MinValue;
            if (IsExcelEmptyValue(value)) return DateTime.MinValue;
            if (IsExcelErrorValue(value)) return DateTime.MinValue;
            if (value is DateTime dt) return dt;
            // Handle all numeric types (int/long/short/byte/float/decimal/…) as Excel
            // OLE serial dates (epoch: 1899-12-30). Convert.ToDouble unifies the
            // dispatch — same approach ToDouble uses. Strings are text, not numbers.
            // NOTE: Excel 1900 leap year bug (serial 60 = fake Feb 29, 1900) is NOT corrected.
            // For dates >= 1900-03-01 (serial >= 61) results are correct.
            // Serial 1-59 are off by 1 day; serial 60 maps to Feb 28 instead of non-existent Feb 29.
            if (value is IConvertible && value is not string && value is not bool)  // P2: bool is not a date (VBA cell semantics)
            {
                double d = Convert.ToDouble(value);
                if (d >= 0 && !double.IsNaN(d) && !double.IsInfinity(d))
                {
                    try { return new DateTime(1899, 12, 30).AddDays(d); }
                    catch (Exception ex) when (ExceptionFilters.IsCatchable(ex))
                    { return DateTime.MinValue; }
                }
                return DateTime.MinValue;
            }
            if (value is string s)
            {
                if (DateTime.TryParse(s, CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out DateTime result))
                    return result;
                return DateTime.MinValue;
            }
            try { return Convert.ToDateTime(value, CultureInfo.InvariantCulture); }
            catch (Exception ex) when (ExceptionFilters.IsCatchable(ex))
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

            if (input is Array typedArr)
            {
                if (typedArr.Rank == 1)
                {
                    // e.g. int[], double[], string[] → convert to object[]
                    var result = new object[typedArr.Length];
                    for (int i = 0; i < typedArr.Length; i++)
                        result[i] = typedArr.GetValue(i)!;
                    return result;
                }
                // review 2026-08-31（深度审查 P2-17）：Rank≥2 强类型数组（如 double[,]）原落入
                // scalar 分支被包装成单元素数组 → 下游塌缩成单个 NaN 或 "Cannot reshape" 报错。
                // 按行优先展平：Array.GetValue(int) 只支持 1D，多维必须用 GetValue(int[])
                // 逐维索引转换（最右维最快变化，与 object[,] 分支一致）。
                var flat = new object[typedArr.Length];
                var idxArr = new int[typedArr.Rank];
                for (int i = 0; i < typedArr.Length; i++)
                {
                    int rem = i;
                    for (int d = typedArr.Rank - 1; d >= 0; d--)
                    {
                        int dim = typedArr.GetLength(d);
                        idxArr[d] = rem % dim;
                        rem /= dim;
                    }
                    flat[i] = typedArr.GetValue(idxArr)!;
                }
                return flat;
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

            if (input is Array multiDimArr && multiDimArr.Rank == 2)
            {
                int rows = multiDimArr.GetLength(0);
                int cols = multiDimArr.GetLength(1);
                var result = new object[rows, cols];
                for (int r = 0; r < rows; r++)
                    for (int c = 0; c < cols; c++)
                        result[r, c] = multiDimArr.GetValue(r, c)!;
                return result;
            }

            // review 2026-09-05（N13）：Rank≥3 数组原落入 Rank≥2 分支，GetValue(int,int) 按 CLR
            // 契约抛未捕获 ArgumentException（且只读 dim 0/1）。与 P2-17（NormalizeTo1D 多维
            // 展平，2026-08-31）同法但保留 2D 形状：按行优先展平为 [n,1] 列向量（最右维最快
            // 变化，与 object[,] 分支一致），不丢数据、不抛裸异常。
            if (input is Array highRankArr && highRankArr.Rank >= 3)
            {
                var result = new object[highRankArr.Length, 1];
                var idxArr = new int[highRankArr.Rank];
                for (int i = 0; i < highRankArr.Length; i++)
                {
                    int rem = i;
                    for (int d = highRankArr.Rank - 1; d >= 0; d--)
                    {
                        int dim = highRankArr.GetLength(d);
                        idxArr[d] = rem % dim;
                        rem /= dim;
                    }
                    result[i, 0] = highRankArr.GetValue(idxArr)!;
                }
                return result;
            }

            // Scalar → 1×1
            return new object[,] { { input } };
        }

        /// <summary>
        /// Like <see cref="NormalizeTo2D"/> but throws on null/empty input instead
        /// of returning null. Use in UDF D() helpers to surface the error as #VALUE!
        /// rather than risking a NullReferenceException from the null-forgiving operator.
        /// </summary>
        internal static object[,] MustNormalizeTo2D(object? input)
        {
            var result = NormalizeTo2D(input);
            if (result == null)
                throw new ArgumentException(ErrorMsg.Get("Input_NullOrEmpty"));
            return result;
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
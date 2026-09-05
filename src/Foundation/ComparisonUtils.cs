using System;
using System.Globalization;
using System.Text;

namespace ExcelFormulaLabs.Foundation
{
    /// <summary>
    /// Type-aware value comparison and deterministic key generation.
    /// Ported from VariantKit.cls: ValuesEqual, Compare, SafeKey.
    /// </summary>
    /// <remarks>
    /// These methods handle the full Excel/VBA variant type system:
    /// Null, Empty, Error, Boolean, Numeric, Date, String, Array, Object.
    /// The comparison semantics match VBA exactly to ensure cross-validation
    /// test compatibility.
    /// </remarks>
    public static class ComparisonUtils
    {
        // ─────────────────────────────────────────────────────────────────
        // ValuesEqual — semantic equality with type awareness
        // ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// Type-aware semantic equality between two values.
        /// </summary>
        /// <param name="a">First value.</param>
        /// <param name="b">Second value.</param>
        /// <param name="epsilon">Relative tolerance for numeric comparison (default 1e-12);
        /// compared as |a−b| &lt; epsilon·max(|a|,|b|). Scale-free — safe for ppm/ppb data.</param>
        /// <returns><c>true</c> if the values are semantically equal.</returns>
        /// <remarks>
        /// Comparison order (matches VBA VariantKit.ValuesEqual exactly):
        ///   1. Both Null  → true
        ///   2. One Null   → false
        ///   3. Both Empty → true
        ///   4. One Empty  → false
        ///   5. Both Error → compare Code
        ///   6. One Error  → false
        ///   7. Boolean    → equal only if BOTH are boolean and same value
        ///   8. Both Date  → compare CDate values
        ///   9. Both Numeric → epsilon comparison
        ///   10. Fallback  → case-sensitive string comparison
        /// </remarks>
        public static bool ValuesEqual(object? a, object? b, double epsilon = 1e-12)
        {
            // 1. Both Null — null and DBNull are treated as equivalent
            bool aNull = a == null || a is DBNull;
            bool bNull = b == null || b is DBNull;
            if (aNull && bNull) return true;
            if (aNull || bNull) return false;

            // 2. Both Empty
            bool aEmpty = InputNormalizer.IsExcelEmptyValue(a);
            bool bEmpty = InputNormalizer.IsExcelEmptyValue(b);
            if (aEmpty && bEmpty) return true;
            if (aEmpty || bEmpty) return false;

            // 3. Both Error — Foundation sentinels compare by code;
            //    Excel-DNA enum errors are all treated as the same error group.
            if (a is ExcelError errA && b is ExcelError errB)
                return errA.Code == errB.Code;
            bool aErr = InputNormalizer.IsExcelErrorValue(a);
            bool bErr = InputNormalizer.IsExcelErrorValue(b);
            if (aErr || bErr)
                return aErr && bErr;

            // 4. Boolean — only equal if BOTH are boolean
            if (a is bool boolA && b is bool boolB)
                return boolA == boolB;
            if (a is bool || b is bool)
                return false;  // Boolean ≠ numeric (VBA treats True = -1, but C# is stricter)

            // 5. Both Dates
            if (a is DateTime dtA && b is DateTime dtB)
                return dtA == dtB;

            // 6. Both Numeric — epsilon comparison
            // Note: IsNumeric returns true for double.NaN/Inf (they ARE doubles).
            // NaN == NaN is treated as true here for consistency with SafeKey (防错原则1).
            if (IsNumeric(a!) && IsNumeric(b!))
            {
                double dA = Convert.ToDouble(a, CultureInfo.InvariantCulture);
                double dB = Convert.ToDouble(b, CultureInfo.InvariantCulture);
                if (double.IsNaN(dA) && double.IsNaN(dB)) return true;
                if (double.IsNaN(dA) || double.IsNaN(dB)) return false;
                // Infinity: Math.Abs(Inf - Inf) = NaN, which would incorrectly return false.
                // Handle Infinity explicitly — same-sign infinities are equal, cross-sign are not.
                if (double.IsInfinity(dA) && double.IsInfinity(dB)) return dA == dB;
                if (double.IsInfinity(dA) || double.IsInfinity(dB)) return false;
                // review 2026-09-05（R04）：原绝对容差 |a−b| < 1e-12 与数据量纲无关，
                // 小量纲数据（ppm/ppb）下 100% 相对差也命中（1.5e-16≈2.5e-16 判真）。
                // 与 review-2026-09-04（reaudit D1）Index 相对容差同族同步：
                // 相等快路径（覆盖 0==0，相对窗口双零下溢）+ 纯相对容差 |a−b| < ε·max(|a|,|b|)。
                if (dA == dB) return true;
                return Math.Abs(dA - dB) <
                    epsilon * Math.Max(Math.Abs(dA), Math.Abs(dB));
            }

            // 7. Fallback: case-sensitive string comparison (= VBA's CStr = operator)
            string sA = Convert.ToString(a, CultureInfo.InvariantCulture) ?? "";
            string sB = Convert.ToString(b, CultureInfo.InvariantCulture) ?? "";
            return string.Equals(sA, sB, StringComparison.Ordinal);
        }

        // ─────────────────────────────────────────────────────────────────
        // Compare — sort comparator with type ordering
        // ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// Compare two values for sorting. Returns -1, 0, or 1.
        /// </summary>
        /// <remarks>
        /// Type ordering (matches VBA VariantKit.Compare exactly):
        ///   Null first → Empty → values → Error last.
        /// Within same type group:
        ///   Numeric → numeric comparison
        ///   Date → chronological
        ///   Other → case-INsensitive string comparison (vbTextCompare)
        /// </remarks>
        public static int Compare(object? a, object? b)
        {
            int orderA = GetSortOrder(a);
            int orderB = GetSortOrder(b);

            if (orderA != orderB)
                return orderA.CompareTo(orderB);

            // Same type group — compare within group
            return CompareSameGroup(a!, b!);
        }

        /// <summary>
        /// Returns a sort-order priority. Lower = sorts earlier.
        /// </summary>
        private static int GetSortOrder(object? value)
        {
            if (value == null || value is DBNull) return 0;          // Null first
            if (InputNormalizer.IsExcelEmptyValue(value)) return 1;  // Empty second
            if (InputNormalizer.IsExcelErrorValue(value)) return 5;   // Error last
            return 2;  // Normal value
        }

        /// <summary>
        /// Compare two values known to be in the same type group (same sort order).
        /// </summary>
        private static int CompareSameGroup(object a, object b)
        {
            if (InputNormalizer.IsExcelErrorValue(a) && InputNormalizer.IsExcelErrorValue(b))
                return 0;  // All errors sort together

            if (a is DateTime dtA && b is DateTime dtB)
                return dtA.CompareTo(dtB);

            // Note: IsNumeric returns true for double.NaN. NaN sorts last (防错原则1).
            if (IsNumeric(a!) && IsNumeric(b!))
            {
                double dA = Convert.ToDouble(a, CultureInfo.InvariantCulture);
                double dB = Convert.ToDouble(b, CultureInfo.InvariantCulture);
                if (double.IsNaN(dA) && double.IsNaN(dB)) return 0;
                if (double.IsNaN(dA)) return 1;
                if (double.IsNaN(dB)) return -1;
                return dA.CompareTo(dB);
            }

            // String fallback: case-INsensitive, ordinal (deterministic across locales)
            string sA = SafeStr(a);
            string sB = SafeStr(b);
            int cmp = string.Compare(sA, sB, StringComparison.OrdinalIgnoreCase);
            return Math.Sign(cmp);  // Normalize to -1/0/1 per method contract
        }

        // ─────────────────────────────────────────────────────────────────
        // SafeKey — deterministic string key for any value
        // ─────────────────────────────────────────────────────────────────

        private const int MaxSafeKeyDepth = 20;

        /// <summary>
        /// Generate a deterministic, type-disambiguated string key for any value.
        /// </summary>
        /// <param name="value">The value to generate a key for.</param>
        /// <param name="depth">Recursion depth guard — throws if exceeded to prevent
        /// stack overflow from circular array references.</param>
        /// <remarks>
        /// Key format (matches VBA VariantKit.SafeKey exactly):
        ///   Null → "Null:##NULL##"
        ///   Empty → "Empty:##EMPTY##"
        ///   Error → "Error:#ERR(code)"
        ///   Numeric → "Numeric:G17-string"
        ///   String → "String:value"
        ///   Boolean → "Boolean:True|False"
        ///   DateTime → "Date:yyyy-MM-dd HH:mm:ss"
        ///   Object → "Object:TypeName:HashCode"
        ///   Array 1D → "Array(N):key1|key2|..."
        ///   Array 2D → "Array2D(R×C):flattened|keys"
        /// </remarks>
        public static string SafeKey(object? value, int depth = 0)
        {
            if (depth > MaxSafeKeyDepth)
                throw new ArgumentException(
                    $"SafeKey: maximum nesting depth ({MaxSafeKeyDepth}) exceeded. " +
                    "The input may contain circular array references.");
            if (value == null || value is DBNull)
                return "Null:##NULL##";

            if (InputNormalizer.IsExcelEmptyValue(value))
                return "Empty:##EMPTY##";

            if (value is ExcelError err)
                return $"Error:#ERR({err.Code})";

            if (value is bool b)
                return b ? "Boolean:True" : "Boolean:False";

            if (value is DateTime dt)
                return $"Date:{dt:yyyy-MM-dd HH:mm:ss}";

            if (value is string s)
                return $"String:{s}";

            if (value is object[] arr1D)
                return Array1DToKey(arr1D, depth + 1);

            if (value is object[,] arr2D)
                return Array2DToKey(arr2D, depth + 1);

            if (IsNumeric(value))
            {
                double d = Convert.ToDouble(value, CultureInfo.InvariantCulture);
                return $"Numeric:{d.ToString("G17", CultureInfo.InvariantCulture)}";
            }

            // review 2026-08-31（深度审查 P2-16）：原 `GetHashCode()` 在 .NET 进程内随机化
            // （string 每进程不同 seed）→ SafeKey 结果不可复现，ARR.UNIQUE 去重结果随进程漂移。
            // 改为 ToString（确定性）；数值/日期/数组等已在上方分支精确处理，此处只剩自定义对象。
            return $"Object:{value.GetType().Name}:{value.ToString() ?? ""}";
        }

        // ── Private helpers ──────────────────────────────────────────────

        /// <summary>Build SafeKey for a 1D array, recursing into elements.</summary>
        private static string Array1DToKey(object[] arr, int depth)
        {
            int len = arr.Length;
            if (len == 0) return "Array(0):##EMPTY##";

            var sb = new StringBuilder();
            sb.Append("Array(").Append(len).Append("):");
            for (int i = 0; i < len; i++)
            {
                if (i > 0) sb.Append('|');
                sb.Append(SafeKey(arr[i], depth));
            }
            return sb.ToString();
        }

        /// <summary>Build SafeKey for a 2D array, flattening row-major.</summary>
        private static string Array2DToKey(object[,] arr, int depth)
        {
            int rows = arr.GetLength(0);
            int cols = arr.GetLength(1);
            if (rows == 0 || cols == 0) return "Array2D(0×0):##EMPTY##";

            var sb = new StringBuilder();
            sb.Append("Array2D(").Append(rows).Append('×').Append(cols).Append("):");
            bool first = true;
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    if (!first) sb.Append('|');
                    sb.Append(SafeKey(arr[r, c], depth));
                    first = false;
                }
            }
            return sb.ToString();
        }

        /// <summary>Safe string conversion — handles errors, null, empty gracefully.</summary>
        private static string SafeStr(object? value)
        {
            if (value == null || value is DBNull) return "";
            if (InputNormalizer.IsExcelEmptyValue(value)) return "";
            if (value is ExcelError err) return $"#ERR({err.Code})";
            return Convert.ToString(value, CultureInfo.InvariantCulture) ?? "";
        }

        /// <summary>
        /// Returns true if the value is a numeric type (int, long, float, double, decimal,
        /// or a numeric string). Does NOT return true for bool, DateTime, or null.
        /// Equivalent to VBA's IsNumeric function.
        /// </summary>
        /// <remarks>
        /// Unlike <see cref="InputNormalizer.IsNumericCell"/>, this variant does not
        /// explicitly reject <c>bool</c> and <c>DateTime</c> — callers
        /// (<see cref="ValuesEqual"/>, <see cref="CompareSameGroup"/>) have already
        /// handled those types before reaching this method, so the extra guards are
        /// unnecessary here.
        /// </remarks>
        internal static bool IsNumeric(object? value)
        {
            if (value == null) return false;
            // Numeric types (incl. double.NaN/Inf): pass through. Convert.ToDouble + IEEE 754
            // comparison handles NaN correctly (NaN ≠ NaN in Math.Abs check).
            // String "NaN"/"Infinity" are rejected — non-finite strings should never compare as numbers.
            if (value is int || value is long || value is float || value is double
                || value is decimal || value is short || value is byte
                || value is sbyte || value is ushort || value is uint || value is ulong)
                return true;
            if (value is string s && s.Trim().Length > 0)
                return double.TryParse(s, NumberStyles.Float | NumberStyles.AllowThousands,
                    CultureInfo.InvariantCulture, out double val)
                    && !double.IsNaN(val) && !double.IsInfinity(val);
            return false;
        }
    }
}
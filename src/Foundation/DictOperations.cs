using System;
using System.Collections.Generic;

namespace ExcelVbaLibraries.Foundation
{
    /// <summary>
    /// Safe Dictionary factory and batch operations.
    /// Ported from DictProxy.cls: Create, FromKeys, ToArray, Merge.
    /// </summary>
    /// <remarks>
    /// VBA's <c>Scripting.Dictionary</c> defaults to case-insensitive text comparison
    /// (<c>vbTextCompare</c> = 1). The C# equivalent is
    /// <see cref="StringComparison.OrdinalIgnoreCase"/>.
    /// All keys are <c>string</c> — numeric/date keys are converted to
    /// deterministic string representations by <see cref="FromKeys"/>.
    /// </remarks>
    public static class DictOperations
    {
        /// <summary>
        /// Create a new Dictionary with the specified key comparison.
        /// Default: <see cref="StringComparison.OrdinalIgnoreCase"/> (matches VBA vbTextCompare).
        /// </summary>
        public static Dictionary<string, object> Create(
            StringComparison compareMode = StringComparison.OrdinalIgnoreCase)
        {
            return new Dictionary<string, object>(ComparerFromMode(compareMode));
        }

        /// <summary>
        /// Create a Dictionary from an array of keys, all mapped to the same value.
        /// </summary>
        /// <param name="keys">Array of key values (any type). Null/Error/Empty/Array/Object keys are skipped.</param>
        /// <param name="defaultValue">Value assigned to each key (default: null).</param>
        /// <param name="compareMode">Key comparison mode.</param>
        /// <returns>A new Dictionary. Duplicate keys: first occurrence wins.</returns>
        public static Dictionary<string, object> FromKeys(
            object[] keys, object? defaultValue = null,
            StringComparison compareMode = StringComparison.OrdinalIgnoreCase)
        {
            var dict = Create(compareMode);
            if (keys == null || keys.Length == 0) return dict;

            foreach (object key in keys)
            {
                if (key == null || key is DBNull) continue;
                if (key is ExcelError) continue;
                if (ReferenceEquals(key, ExcelEmpty.Value)) continue;
                if (key is Array) continue;

                string keyStr = KeyToString(key);
                if (!dict.ContainsKey(keyStr))
                    dict.Add(keyStr, defaultValue ?? ExcelEmpty.Value);
            }
            return dict;
        }

        /// <summary>
        /// Export a Dictionary as a 2D object array: column 0 = keys, column 1 = values.
        /// </summary>
        /// <returns>An <c>object[n, 2]</c>, or <c>null</c> for null/empty dict.</returns>
        public static object[,]? ToArray(Dictionary<string, object>? dict)
        {
            if (dict == null || dict.Count == 0) return null;
            var result = new object[dict.Count, 2];
            int row = 0;
            foreach (var kvp in dict)
            {
                result[row, 0] = kvp.Key;
                result[row, 1] = kvp.Value ?? ExcelEmpty.Value;
                row++;
            }
            return result;
        }

        /// <summary>
        /// Merge two dictionaries into a new one.
        /// </summary>
        /// <param name="a">First dictionary (can be null).</param>
        /// <param name="b">Second dictionary (can be null).</param>
        /// <param name="overwrite">If true, b's values overwrite a's for shared keys.</param>
        public static Dictionary<string, object> Merge(
            Dictionary<string, object>? a,
            Dictionary<string, object>? b,
            bool overwrite = false)
        {
            var result = new Dictionary<string, object>(
                a?.Comparer ?? b?.Comparer ?? StringComparer.OrdinalIgnoreCase);
            if (a != null)
                foreach (var kvp in a) result[kvp.Key] = kvp.Value;
            if (b != null)
                foreach (var kvp in b)
                    if (overwrite || !result.ContainsKey(kvp.Key))
                        result[kvp.Key] = kvp.Value;
            return result;
        }

        private static string KeyToString(object value)
        {
            if (value is string s) return s;
            if (value is double d) return double.IsNaN(d) ? "NaN" : double.IsInfinity(d) ? (d > 0 ? "+Inf" : "-Inf") : d.ToString("G17", System.Globalization.CultureInfo.InvariantCulture);
            if (value is float f) return ((double)f).ToString("G17", System.Globalization.CultureInfo.InvariantCulture);
            if (value is decimal m) return ((double)m).ToString("G17", System.Globalization.CultureInfo.InvariantCulture);
            if (value is int i) return i.ToString(System.Globalization.CultureInfo.InvariantCulture);
            if (value is long l) return l.ToString(System.Globalization.CultureInfo.InvariantCulture);
            if (value is short s16) return s16.ToString(System.Globalization.CultureInfo.InvariantCulture);
            if (value is byte b8) return b8.ToString(System.Globalization.CultureInfo.InvariantCulture);
            if (value is DateTime dt) return dt.ToString("yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);
            if (value is bool b) return b ? "TRUE" : "FALSE";
            return Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture) ?? "";
        }

        private static IEqualityComparer<string> ComparerFromMode(StringComparison mode) => mode switch
        {
            StringComparison.OrdinalIgnoreCase => StringComparer.OrdinalIgnoreCase,
            StringComparison.Ordinal => StringComparer.Ordinal,
            StringComparison.CurrentCultureIgnoreCase => StringComparer.CurrentCultureIgnoreCase,
            StringComparison.CurrentCulture => StringComparer.CurrentCulture,
            StringComparison.InvariantCultureIgnoreCase => StringComparer.InvariantCultureIgnoreCase,
            StringComparison.InvariantCulture => StringComparer.InvariantCulture,
            _ => StringComparer.OrdinalIgnoreCase,
        };
    }
}

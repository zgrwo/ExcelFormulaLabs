using System;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace ExcelVbaLibraries.Foundation
{
    /// <summary>
    /// Filter condition evaluation — determines whether an element passes a
    /// comparison against a match value. Ported from VariantKit.FilterPasses.
    /// </summary>
    /// <remarks>
    /// Supported operators:
    ///   "=","&lt;&gt;","&lt;","&lt;=","&gt;","&gt;=" — standard comparisons
    ///   "contains","notcontains" — case-insensitive substring
    ///   "startswith","endswith" — case-insensitive prefix/suffix
    ///   "regex" — regular expression (IgnoreCase)
    ///   "isblank","isnotblank" — blank/empty detection
    /// Error/Null/Object/Array elements return false (except isblank/isnotblank).
    /// </remarks>
    public static class FilterUtils
    {
        /// <summary>
        /// Evaluate whether <paramref name="element"/> passes the filter.
        /// </summary>
        public static bool FilterPasses(object? element, object? matchValue, string op)
        {
            string opLower = op.ToLowerInvariant();
            switch (opLower)
            {
                case "isblank":    return IsBlank(element);
                case "isnotblank": return !IsBlank(element);
            }

            // All other operators: reject Error/Null/Object/Array elements and matchValues
            if (element == null || element is DBNull) return false;
            if (element is ExcelError) return false;
            if (element is Array) return false;
            if (element is not string && Marshal.IsComObject(element)) return false;

            if (matchValue == null || matchValue is DBNull) return false;
            if (matchValue is ExcelError) return false;
            if (matchValue is Array) return false;
            if (matchValue is not string && Marshal.IsComObject(matchValue)) return false;

            return opLower switch
            {
                "=" => ComparisonUtils.ValuesEqual(element, matchValue),
                "<>" => !ComparisonUtils.ValuesEqual(element, matchValue),
                "<" => Math.Sign(ComparisonUtils.Compare(element, matchValue)) == -1,
                "<=" => ComparisonUtils.Compare(element, matchValue) <= 0,
                ">" => Math.Sign(ComparisonUtils.Compare(element, matchValue)) == 1,
                ">=" => ComparisonUtils.Compare(element, matchValue) >= 0,
                "contains" => Contains(element, matchValue),
                "notcontains" => !Contains(element, matchValue),
                "startswith" => StartsEndsWith(element, matchValue, true),
                "endswith" => StartsEndsWith(element, matchValue, false),
                "regex" => RegexMatch(element, matchValue),
                _ => false,
            };
        }

        private static bool IsBlank(object? value)
        {
            if (value == null || value is DBNull) return true;
            if (InputNormalizer.IsExcelMissing(value)) return true;
            if (ReferenceEquals(value, ExcelEmpty.Value)) return true;
            if (value is string s) return s.Trim().Length == 0;
            return false;
        }

        private static bool Contains(object element, object matchValue)
        {
            string sEl = InputNormalizer.ToString(element);
            string sMv = InputNormalizer.ToString(matchValue);
            return sEl.IndexOf(sMv, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool StartsEndsWith(object element, object matchValue, bool isStart)
        {
            string sEl = InputNormalizer.ToString(element);
            string sMv = InputNormalizer.ToString(matchValue);
            if (sEl.Length < sMv.Length) return false;  // VBA: False if shorter
            return isStart
                ? sEl.StartsWith(sMv, StringComparison.OrdinalIgnoreCase)
                : sEl.EndsWith(sMv, StringComparison.OrdinalIgnoreCase);
        }

        private static bool RegexMatch(object element, object matchValue)
        {
            string pattern = InputNormalizer.ToString(matchValue);
            if (string.IsNullOrEmpty(pattern)) return false;
            try
            {
                return Regex.IsMatch(
                    InputNormalizer.ToString(element),
                    pattern,
                    RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
                    TimeSpan.FromSeconds(5));
            }
            catch (Exception ex) when (ex is not OutOfMemoryException
                and not StackOverflowException
                and not AccessViolationException)
            {
                return false;  // Invalid pattern → silent false (VBA behaviour)
            }
        }
    }
}

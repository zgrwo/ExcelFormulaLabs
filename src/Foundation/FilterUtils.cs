using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace ExcelFormulaLabs.Foundation
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
        // Regex objects are cached to avoid re-parsing the same pattern per element.
        // .NET's internal static Regex cache is limited to 15 entries (Regex.CacheSize)
        // and shared globally; this dedicated cache ensures FilterUtils patterns don't
        // get evicted by other Regex users (RegexCore, etc.).
        private static readonly ConcurrentDictionary<string, Regex> RegexCache = new();
        private const int MaxCachedRegex = 64;

        /// <summary>Known comparison operator names (lowercase). isblank/isnotblank are
        /// handled before element-type guards (they accept null/error elements).</summary>
        private static readonly System.Collections.Generic.HashSet<string> KnownOperators =
            new(System.StringComparer.Ordinal)
            {
                "=", "<>", "<", "<=", ">", ">=",
                "contains", "notcontains", "startswith", "endswith", "regex",
            };

        /// <summary>
        /// Clear the regex cache. Safe to call at any time;
        /// subsequent filter operations will recompile patterns as needed.
        /// Called by add-in AutoClose on unload.
        /// </summary>
        public static void ClearRegexCache() => RegexCache.Clear();

        /// <summary>
        /// Evaluate whether <paramref name="element"/> passes the filter.
        /// </summary>
        public static bool FilterPasses(object? element, object? matchValue, string op)
        {
            // a null operator would produce an NRE swallowed by WrapError into a misleading
            // #VALUE!; reject it explicitly.
            if (string.IsNullOrEmpty(op))
                throw new ArgumentException("Filter operator must not be null or empty.");
            string opLower = op.ToLowerInvariant();
            switch (opLower)
            {
                case "isblank": return IsBlank(element);
                case "isnotblank": return !IsBlank(element);
            }
            // 未知 operator 须显式报错（不得静默返回 false——整列被过滤光，用户无法
            // 区分"无匹配"与"参数错误"）。与 null operator 的显式拒绝保持一致：
            // 未知 op → ArgumentException → UDF #VALUE!。
            if (!KnownOperators.Contains(opLower))
                throw new ArgumentException(
                    $"Unknown filter operator '{op}'. Supported: =, <>, <, <=, >, >=, " +
                    "contains, notcontains, startswith, endswith, regex, isblank, isnotblank.");

            // All other operators: reject Error/Null/Object/Array elements and matchValues
            if (element == null || element is DBNull) return false;
            if (InputNormalizer.IsExcelErrorValue(element)) return false;
            if (element is Array) return false;
            if (element is not string && Marshal.IsComObject(element)) return false;

            if (matchValue == null || matchValue is DBNull) return false;
            if (InputNormalizer.IsExcelErrorValue(matchValue)) return false;
            if (matchValue is Array) return false;
            if (matchValue is not string && Marshal.IsComObject(matchValue)) return false;

            // IEEE 754: NaN is unordered — reject from ordered comparisons
            if (opLower is "<" or "<=" or ">" or ">=")
            {
                if (element is double de && double.IsNaN(de)) return false;
                if (matchValue is double dm && double.IsNaN(dm)) return false;
            }

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
            if (InputNormalizer.IsExcelEmptyValue(value)) return true;
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

        private static readonly TimeSpan RegexTimeout = TimeSpan.FromSeconds(5);

        private static bool RegexMatch(object element, object matchValue)
        {
            string pattern = InputNormalizer.ToString(matchValue);
            if (string.IsNullOrEmpty(pattern)) return false;
            // Guard against overly-long patterns (mirrors RegexCore.MaxPatternLength)
            const int maxPatternLength = 10000;
            if (pattern.Length > maxPatternLength) return false;
            try
            {
                RegexBudget.ThrowIfExhausted("Filter regex");
                var remaining = RegexBudget.Remaining;
                Regex regex;
                if (remaining < RegexTimeout)
                {
                    // 剩余预算小于单次 5s → 用剩余额度即时构造（不进缓存）：数组后续格子
                    // 不会各自再耗 5s 放大总时长（R1-4）。cached Regex 的超时无法逐次收紧。
                    regex = new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
                        remaining);
                }
                else
                {
                    regex = RegexCache.GetOrAdd(pattern, p =>
                        new Regex(p, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
                            RegexTimeout));
                    // evict OUTSIDE the GetOrAdd factory — eviction inside the factory races
                    // other threads (non-deterministic victim, possible eviction of a pattern
                    // another thread just cached).
                    // 单条驱逐在高并发独特模式下可短暂超上限 → 循环清至预算内
                    // （受害选择保持 first、永不驱逐本次 pattern，防自逐与死循环）。
                    while (RegexCache.Count > MaxCachedRegex)
                    {
                        // P3-9：旧实现只看 keys.First()，victim==本次 pattern 即 break
                        // （枚举顺序恰把本次放首位时会持续超预算）。改为找首个非本次 pattern。
                        string? victim = null;
                        foreach (var k in RegexCache.Keys)
                        {
                            if (!string.Equals(k, pattern, StringComparison.Ordinal)) { victim = k; break; }
                        }
                        if (victim == null) break;
                        RegexCache.TryRemove(victim, out _);
                    }
                }
                return regex.IsMatch(InputNormalizer.ToString(element));
            }
            catch (RegexMatchTimeoutException)
            {
                // 超时不得静默判 false（整列被过滤光的假阴性）；显式上抛 → ARR.FILTER #VALUE!。
                throw;
            }
            catch (Exception ex) when (ExceptionFilters.IsCatchable(ex))
            {
                return false;  // Invalid pattern → silent false (VBA behaviour)
            }
        }
    }
}
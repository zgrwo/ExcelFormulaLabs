using System;
using System.Text.RegularExpressions;
using ExcelVbaLibraries.Foundation;

namespace ExcelVbaLibraries.DataToolkit
{
    /// <summary>Regex operations: match, replace, split, capture groups. Ported from RegexUtils.bas.</summary>
    internal static class RegexCore
    {
        private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);
        internal static bool RegexTest(string i, string p, bool ic=true) =>
            Regex.IsMatch(i, p, F(ic), Timeout);
        internal static long RegexCount(string i, string p, bool ic=true) =>
            Regex.Matches(i, p, F(ic), Timeout).Count;
        internal static string RegexMatch(string i, string p, bool ic=true)
            => RegexMatch(i, p, 1, ic);

        /// <summary>
        /// Nth regex match. n=1 (default) = first match, n=-1 = last match.
        /// Returns "" if |n| exceeds match count or n is out of range.
        /// n=1 uses Regex.Match (single-scan) for the common fast path;
        /// other n values use Regex.Matches to locate the target index.
        /// </summary>
        internal static string RegexMatch(string i, string p, long n, bool ic=true)
        {
            if (n == 0) n = 1;
            // Fast path: first match — Regex.Match scans only until the first hit
            if (n == 1) { var m = Regex.Match(i, p, F(ic), Timeout); return m.Success ? m.Value : ""; }
            // General path: need a specific index → compute all matches
            var mc = Regex.Matches(i, p, F(ic), Timeout);
            if (mc.Count == 0) return "";
            int idx = n > 0 ? (int)n - 1 : mc.Count + (int)n;
            if (idx < 0 || idx >= mc.Count) return "";
            return mc[idx].Value;
        }
        internal static string[] RegexMatchAll(string i, string p, bool ic=true)
        { var mc=Regex.Matches(i,p,F(ic),Timeout); var r=new string[mc.Count]; for(int j=0;j<mc.Count;j++)r[j]=mc[j].Value; return r; }
        internal static string RegexReplace(string i, string p, string r, bool ic=true)
            => RegexReplace(i, p, r, 0, ic);

        /// <summary>
        /// Replace the nth regex match. n=0 (default) = replace all.
        /// n=1 = first match, n=-1 = last match.
        /// Returns original string unchanged if |n| exceeds match count.
        /// n=1 uses Regex.Match (single-scan) for the common fast path.
        /// </summary>
        internal static string RegexReplace(string i, string p, string r, long n, bool ic=true)
        {
            if (n == 0) return Regex.Replace(i, p, r, F(ic), Timeout);
            // Fast path: replace first match only
            if (n == 1) { var m = Regex.Match(i, p, F(ic), Timeout); return m.Success ? i.Substring(0, m.Index) + r + i.Substring(m.Index + m.Length) : i; }
            // General path: need a specific index → compute all matches
            var mc = Regex.Matches(i, p, F(ic), Timeout);
            if (mc.Count == 0) return i;
            int idx = n > 0 ? (int)n - 1 : mc.Count + (int)n;
            if (idx < 0 || idx >= mc.Count) return i;
            var match = mc[idx];
            return i.Substring(0, match.Index) + r + i.Substring(match.Index + match.Length);
        }

        internal static string[] RegexSplit(string i, string p, bool ic=true)
            => RegexSplit(i, p, 0, ic);

        /// <summary>
        /// Split by regex with optional max-split limit.
        /// n=0 (default) = split at all matches (unlimited).
        /// n>0 = split at most n times (yields at most n+1 parts).
        /// Uses lazy foreach over MatchCollection — early exit avoids scanning beyond n splits.
        /// </summary>
        internal static string[] RegexSplit(string i, string p, long n, bool ic=true)
        {
            if (n <= 0) return Regex.Split(i, p, F(ic), Timeout);
            var result = new System.Collections.Generic.List<string>((int)n + 1);
            int pos = 0, splitCount = 0;
            foreach (Match m in Regex.Matches(i, p, F(ic), Timeout))
            {
                if (splitCount >= n) break;
                result.Add(i.Substring(pos, m.Index - pos));
                pos = m.Index + m.Length;
                splitCount++;
            }
            result.Add(i.Substring(pos));
            return result.ToArray();
        }
        /// <summary>
        /// Capture groups from first regex match.
        /// Returns object[2,n]: row0 = group names (integer strings for unnamed groups,
        /// or actual names for named groups like (?&lt;year&gt;\d{4})), row1 = captured values.
        /// [0] = full match. Returns empty 0×0 array on no match.
        /// </summary>
        internal static object[,] RegexCaptureGroups(string i, string p, bool ic=true)
        {
            var m = Regex.Match(i, p, FC(ic), Timeout);
            if (!m.Success) return new object[0, 0];
            var r = new object[2, m.Groups.Count];
            for (int j = 0; j < m.Groups.Count; j++)
            {
                r[0, j] = m.Groups[j].Name;
                r[1, j] = m.Groups[j].Value;
            }
            return r;
        }
        internal static string RegexEscape(string l) => Regex.Escape(l);
        private static RegexOptions F(bool ic) =>
            (ic ? RegexOptions.IgnoreCase : RegexOptions.None) | RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture;

        /// <summary>Regex options WITH capture groups — only for RegexCaptureGroups.</summary>
        private static RegexOptions FC(bool ic) =>
            (ic ? RegexOptions.IgnoreCase : RegexOptions.None) | RegexOptions.CultureInvariant;
    }
}

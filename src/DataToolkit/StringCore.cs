using System;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
#if !NET8_0_OR_GREATER
using System.Threading;
#endif
using ExcelFormulaLabs.Foundation;

namespace ExcelFormulaLabs.DataToolkit
{
    /// <summary>String manipulation: encoding, validation, distance, UUID, URL, formatting. Ported from StringUtils.bas.</summary>
    internal static class StringCore
    {
        private static readonly Regex WhitespaceRx = new(@"\s+", RegexOptions.Compiled, TimeSpan.FromSeconds(5));
        private static readonly Regex HtmlTagRx = new(@"<[^>]+>", RegexOptions.Compiled, TimeSpan.FromSeconds(5));

        internal static string ReverseString(string t) { t ??= ""; var a = t.ToCharArray(); Array.Reverse(a); return new string(a); }
        internal static string NormalizeWhitespace(string t) { t ??= ""; return WhitespaceRx.Replace(t.Trim(), " "); }
        internal static string StripHtml(string t) { t ??= ""; return HtmlTagRx.Replace(t, ""); }
        internal static string ToTitleCase(string t) { t ??= ""; return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(t.ToLowerInvariant()); }
        internal static string RemoveChars(string t, string chars) { t ??= ""; var set = new System.Collections.Generic.HashSet<char>(chars); var sb = new System.Text.StringBuilder(t.Length); foreach (char c in t) if (!set.Contains(c)) sb.Append(c); return sb.ToString(); }
        internal static string KeepChars(string t, string keep) { t ??= ""; var set = new System.Collections.Generic.HashSet<char>(keep); var sb = new StringBuilder(t.Length); foreach (char c in t) if (set.Contains(c)) sb.Append(c); return sb.ToString(); }

        internal static string PadLeft(string t, int len, char pad = ' ')
        { if (t.Length >= len) return t; return new string(pad, len - t.Length) + t; }

        internal static string PadRight(string t, int len, char pad = ' ')
        { if (t.Length >= len) return t; return t + new string(pad, len - t.Length); }

        internal static string Truncate(string t, int max, string suffix = "...")
        { t ??= ""; if (max <= 0) return ""; if (t.Length <= max) return t; int keep = max - suffix.Length; if (keep <= 0) return t.Substring(0, max); return t.Substring(0, keep) + suffix; }

        internal static long CountSubstring(string t, string s, bool cs = true)
        { if(string.IsNullOrEmpty(s)||string.IsNullOrEmpty(t))return 0; int c=0,i=0; var m=cs?0:1; while((i=t.IndexOf(s,i,m==0?StringComparison.Ordinal:StringComparison.OrdinalIgnoreCase))>=0){c++;i+=s.Length;} return c; }

        internal static bool StartsWithStr(string t, string p, bool cs = true)
        { t ??= ""; return t.StartsWith(p, cs ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase); }

        internal static bool EndsWithStr(string t, string s, bool cs = true)
        { t ??= ""; return t.EndsWith(s, cs ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase); }

        internal static string LeftOf(string t, string d, long n=1)
        { t ??= ""; int i=NthIdx(t,d,n); return i<0?t:t.Substring(0,i); }

        internal static string RightOf(string t, string d, long n=1)
        { t ??= ""; int i=NthIdx(t,d,n); return i<0?t:t.Substring(i+d.Length); }

        internal static string ExtractBetween(string t, string l, string r, long n=1, bool inc=false)
        { t ??= ""; int s=NthIdx(t,l,n); if(s<0)return""; int e=t.IndexOf(r,s+l.Length); if(e<0)return""; return inc?t.Substring(s,e-s+r.Length):t.Substring(s+l.Length,e-s-l.Length); }

        internal static string NthWord(string t, long n)
        {
            t ??= "";
            if (n == 0) n = 1;                            // default → first word
            var w = t.Split((char[])null!, StringSplitOptions.RemoveEmptyEntries);
            if (w.Length == 0) return "";
            if (n < 0)                                    // n=-1 → last word, n=-2 → second-to-last
            {
                long idx = w.Length + n;                  // -1 → Length-1
                return idx >= 0 ? w[idx] : "";
            }
            return n <= w.Length ? w[n - 1] : "";
        }

        internal static string CommonPrefix(string a, string b, bool cs=true)
        { a ??= ""; b ??= ""; int i=0; while(i<a.Length&&i<b.Length&&(cs?a[i]==b[i]:char.ToUpperInvariant(a[i])==char.ToUpperInvariant(b[i])))i++; return a.Substring(0,i); }

        internal static string TextJoin(string d, bool skip, string[] v)
        { return string.Join(d, skip?v.Where(x=>!string.IsNullOrEmpty(x)):v); }

        internal static long LevenshteinDistance(string a, string b)
        { a ??= ""; b ??= ""; int na=a.Length,nb=b.Length; var d0=new int[nb+1]; var d1=new int[nb+1]; for(int j=0;j<=nb;j++)d0[j]=j;
          for(int i=1;i<=na;i++){ d1[0]=i; for(int j=1;j<=nb;j++)d1[j]=Math.Min(Math.Min(d0[j]+1,d1[j-1]+1),d0[j-1]+(a[i-1]==b[j-1]?0:1)); var t=d0;d0=d1;d1=t; } return d0[nb]; }

        internal static string Soundex(string t)
        { if(string.IsNullOrEmpty(t))return""; char f=char.ToUpperInvariant(t[0]); var sb=new StringBuilder();sb.Append(f);char pc=Sdx(f);
          for(int i=1;i<t.Length&&sb.Length<4;i++){char c=Sdx(char.ToUpperInvariant(t[i])); if(c!='0'&&c!=pc){sb.Append(c);pc=c;}} while(sb.Length<4)sb.Append('0'); return sb.ToString(); }
        private static char Sdx(char c)=>c switch{'B'or'F'or'P'or'V'=>'1','C'or'G'or'J'or'K'or'Q'or'S'or'X'or'Z'=>'2','D'or'T'=>'3','L'=>'4','M'or'N'=>'5','R'=>'6',_=>'0'};

        internal static string UrlEncode(string t)=>Uri.EscapeDataString(t??"");
        internal static string UrlDecode(string t)=>Uri.UnescapeDataString(t??"");
        internal static string HtmlEncode(string t)=>System.Net.WebUtility.HtmlEncode(t??"");
        internal static string HtmlDecode(string t)=>System.Net.WebUtility.HtmlDecode(t??"");
        internal static string Base64Encode(string t)=>Convert.ToBase64String(Encoding.UTF8.GetBytes(t??""));
        internal static string Base64Decode(string t)=>Encoding.UTF8.GetString(Convert.FromBase64String(t??""));

        /// <summary>
        /// Format a value using a .NET format string or specifier.
        /// For numeric/DateTime values, standard format specifiers (e.g. "D4", "P0", "yyyy-MM-dd")
        /// are applied directly so they work correctly.
        /// If <paramref name="fmt"/> already contains '{' it is used as a composite format string verbatim;
        /// otherwise it is wrapped as "{0:fmt}".
        /// </summary>
        internal static string FormatValue(object? value, string fmt)
        {
            if (string.IsNullOrEmpty(fmt)) return value?.ToString() ?? "";
            string fs = fmt.Contains('{') ? fmt : $"{{0:{fmt}}}";
            try
            {
                if (value is double d) return string.Format(fs, d);
                if (value is int i) return string.Format(fs, i);
                if (value is long l) return string.Format(fs, l);
                if (value is float f) return string.Format(fs, f);
                if (value is decimal m) return string.Format(fs, m);
                if (value is DateTime dt) return string.Format(fs, dt);
                return string.Format(fs, value);
            }
            catch (Exception ex) when (ex is not OutOfMemoryException
                and not StackOverflowException
                and not AccessViolationException)
            {
                // Format specifier incompatible with the value's runtime type
                // (e.g. "D4" applied to a double from Excel). Return the raw value.
                System.Diagnostics.Debug.WriteLine(
                    $"[FormatValue] Failed to format '{value?.GetType().Name}' with '{fmt}': {ex.Message}");
                return value?.ToString() ?? "";
            }
        }

        internal static string UUID()=>Guid.NewGuid().ToString();
        private const string DefaultCharset = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

#if NET8_0_OR_GREATER
        internal static string RandomString(long len=8, string? cs=null)
        { if (string.IsNullOrEmpty(cs)) cs = DefaultCharset; var sb = new StringBuilder((int)len); for (int i = 0; i < len; i++) sb.Append(cs[Random.Shared.Next(cs.Length)]); return sb.ToString(); }
#else
        private static readonly ThreadLocal<Random> _rng = new(() => new Random());
        internal static string RandomString(long len=8, string? cs=null)
        { if (string.IsNullOrEmpty(cs)) cs = DefaultCharset; var sb = new StringBuilder((int)len); var r = _rng.Value!; for (int i = 0; i < len; i++) sb.Append(cs[r.Next(cs.Length)]); return sb.ToString(); }
#endif

        internal static bool IsNullOrEmptyStr(string? t)=>string.IsNullOrEmpty(t);
        internal static bool IsNullOrWhitespaceStr(string? t)=>string.IsNullOrWhiteSpace(t);
        internal static string Coalesce(string? p,string f)=>p??f;

        private static int NthIdx(string t, string s, long n)
        {
            if (string.IsNullOrEmpty(t)) return -1;    // empty string → no match
            if (n == 0) n = 1;                         // default → first occurrence
            if (n < 0)
            {                                          // n=-1 → last, n=-2 → second-to-last, etc.
                long absN = -n;
                int i = t.Length;
                for (long j = 0; j < absN; j++)
                {
                    i = t.LastIndexOf(s, i - 1);       // search backward
                    if (i < 0) return -1;
                }
                return i;
            }
            // n > 0: forward search (original logic preserved)
            int idx = -1;
            for (long j = 0; j < n; j++)
            {
                idx = t.IndexOf(s, idx + 1);
                if (idx < 0) return -1;
            }
            return idx;
        }
    }
}

using System;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
#if !NET8_0_OR_GREATER
using System.Threading;
#endif
using ExcelVbaLibraries.Foundation;

namespace ExcelVbaLibraries.DataToolkit
{
    /// <summary>String manipulation: encoding, validation, distance, UUID, URL, formatting. Ported from StringUtils.bas.</summary>
    internal static class StringCore
    {
        private static readonly Regex WhitespaceRx = new(@"\s+", RegexOptions.Compiled, TimeSpan.FromSeconds(5));
        private static readonly Regex HtmlTagRx = new(@"<[^>]+>", RegexOptions.Compiled, TimeSpan.FromSeconds(5));

        internal static string ReverseString(string t)
        { var a = t.ToCharArray(); Array.Reverse(a); return new string(a); }

        internal static string NormalizeWhitespace(string t) =>
            WhitespaceRx.Replace(t.Trim(), " ");

        internal static string StripHtml(string t) =>
            HtmlTagRx.Replace(t, "");

        internal static string ToTitleCase(string t) =>
            CultureInfo.InvariantCulture.TextInfo.ToTitleCase(t.ToLowerInvariant());

        internal static string RemoveChars(string t, string chars)
        { var set = new System.Collections.Generic.HashSet<char>(chars); var sb = new System.Text.StringBuilder(t.Length); foreach (char c in t) if (!set.Contains(c)) sb.Append(c); return sb.ToString(); }

        internal static string KeepChars(string t, string keep)
        { var set = new System.Collections.Generic.HashSet<char>(keep); var sb = new StringBuilder(); foreach (char c in t) if (set.Contains(c)) sb.Append(c); return sb.ToString(); }

        internal static string PadLeft(string t, int len, char pad = ' ')
        { if (t.Length >= len) return t; return new string(pad, len - t.Length) + t; }

        internal static string PadRight(string t, int len, char pad = ' ')
        { if (t.Length >= len) return t; return t + new string(pad, len - t.Length); }

        internal static string Truncate(string t, int max, string suffix = "...")
        { if (max <= 0) return ""; if (t.Length <= max) return t; int keep = max - suffix.Length; if (keep <= 0) return t.Substring(0, max); return t.Substring(0, keep) + suffix; }

        internal static long CountSubstring(string t, string s, bool cs = true)
        { if(string.IsNullOrEmpty(s)||string.IsNullOrEmpty(t))return 0; int c=0,i=0; var m=cs?0:1; while((i=t.IndexOf(s,i,m==0?StringComparison.Ordinal:StringComparison.OrdinalIgnoreCase))>=0){c++;i+=s.Length;} return c; }

        internal static bool StartsWithStr(string t, string p, bool cs = true) =>
            t.StartsWith(p, cs ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase);

        internal static bool EndsWithStr(string t, string s, bool cs = true) =>
            t.EndsWith(s, cs ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase);

        internal static string LeftOf(string t, string d, long n=1)
        { int i=NthIdx(t,d,n); return i<0?t:t.Substring(0,i); }

        internal static string RightOf(string t, string d, long n=1)
        { int i=NthIdx(t,d,n); return i<0?t:t.Substring(i+d.Length); }

        internal static string ExtractBetween(string t, string l, string r, long n=1, bool inc=false)
        { int s=NthIdx(t,l,n); if(s<0)return""; int e=t.IndexOf(r,s+l.Length); if(e<0)return""; return inc?t.Substring(s,e-s+r.Length):t.Substring(s+l.Length,e-s-l.Length); }

        internal static string NthWord(string t, long n)
        { var w=t.Split((char[])null!,StringSplitOptions.RemoveEmptyEntries); return n>0&&n<=w.Length?w[n-1]:""; }

        internal static string CommonPrefix(string a, string b, bool cs=true)
        { int i=0; while(i<a.Length&&i<b.Length&&(cs?a[i]==b[i]:char.ToUpperInvariant(a[i])==char.ToUpperInvariant(b[i])))i++; return a.Substring(0,i); }

        internal static string TextJoin(string d, bool skip, string[] v)
        { return string.Join(d, skip?v.Where(x=>!string.IsNullOrEmpty(x)):v); }

        internal static long LevenshteinDistance(string a, string b)
        { int na=a.Length,nb=b.Length; var prev=new int[nb+1]; for(int j=0;j<=nb;j++)prev[j]=j;
          for(int i=1;i<=na;i++){ var curr=new int[nb+1]; curr[0]=i;
          for(int j=1;j<=nb;j++)curr[j]=Math.Min(Math.Min(prev[j]+1,curr[j-1]+1),prev[j-1]+(a[i-1]==b[j-1]?0:1)); prev=curr; } return prev[nb]; }

        internal static string Soundex(string t)
        { if(string.IsNullOrEmpty(t))return""; char f=char.ToUpperInvariant(t[0]); var sb=new StringBuilder();sb.Append(f);char pc=Sdx(f);
          for(int i=1;i<t.Length&&sb.Length<4;i++){char c=Sdx(char.ToUpperInvariant(t[i])); if(c!='0'&&c!=pc){sb.Append(c);pc=c;}} while(sb.Length<4)sb.Append('0'); return sb.ToString(); }
        private static char Sdx(char c)=>c switch{'B'or'F'or'P'or'V'=>'1','C'or'G'or'J'or'K'or'Q'or'S'or'X'or'Z'=>'2','D'or'T'=>'3','L'=>'4','M'or'N'=>'5','R'=>'6',_=>'0'};

        internal static string UrlEncode(string t)=>Uri.EscapeDataString(t);
        internal static string UrlDecode(string t)=>Uri.UnescapeDataString(t);
        internal static string HtmlEncode(string t)=>System.Net.WebUtility.HtmlEncode(t);
        internal static string HtmlDecode(string t)=>System.Net.WebUtility.HtmlDecode(t);
        internal static string Base64Encode(string t)=>Convert.ToBase64String(Encoding.UTF8.GetBytes(t));
        internal static string Base64Decode(string t)=>Encoding.UTF8.GetString(Convert.FromBase64String(t));

        internal static string UUID()=>Guid.NewGuid().ToString();
#if NET8_0_OR_GREATER
        internal static string RandomString(long len=8,string? cs=null)
        { cs??="ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789"; var sb=new StringBuilder((int)len); for(int i=0;i<len;i++)sb.Append(cs[Random.Shared.Next(cs.Length)]); return sb.ToString(); }
#else
        private static readonly ThreadLocal<Random> _rng = new(() => new Random());
        internal static string RandomString(long len=8,string? cs=null)
        { cs??="ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789"; var sb=new StringBuilder((int)len); var r=_rng.Value!; for(int i=0;i<len;i++)sb.Append(cs[r.Next(cs.Length)]); return sb.ToString(); }
#endif

        internal static bool IsNullOrEmptyStr(string? t)=>string.IsNullOrEmpty(t);
        internal static bool IsNullOrWhitespaceStr(string? t)=>string.IsNullOrWhiteSpace(t);
        internal static string Coalesce(string? p,string f)=>p??f;

        private static int NthIdx(string t,string s,long n)
        { if(n<1)return -1; int i=-1; for(long j=0;j<n;j++){i=t.IndexOf(s,i+1);if(i<0)return -1;} return i; }
    }
}

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
        { var m=Regex.Match(i,p,F(ic),Timeout); return m.Success?m.Value:""; }
        internal static string[] RegexMatchAll(string i, string p, bool ic=true)
        { var mc=Regex.Matches(i,p,F(ic),Timeout); var r=new string[mc.Count]; for(int j=0;j<mc.Count;j++)r[j]=mc[j].Value; return r; }
        internal static string RegexReplace(string i, string p, string r, bool ic=true) =>
            Regex.Replace(i, p, r, F(ic), Timeout);
        internal static string[] RegexSplit(string i, string p, bool ic=true) =>
            Regex.Split(i, p, F(ic), Timeout);
        internal static string[] RegexCaptureGroups(string i, string p, bool ic=true)
        { var m=Regex.Match(i,p,F(ic),Timeout); if(!m.Success)return Array.Empty<string>(); var r=new string[m.Groups.Count]; for(int j=0;j<m.Groups.Count;j++)r[j]=m.Groups[j].Value; return r; }
        internal static string RegexEscape(string l) => Regex.Escape(l);
        private static RegexOptions F(bool ic) => (ic?RegexOptions.IgnoreCase:RegexOptions.None)|RegexOptions.CultureInvariant;
    }
}

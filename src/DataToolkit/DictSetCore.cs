using System;
using System.Collections.Generic;
using System.Linq;
using ExcelFormulaLabs.Foundation;

namespace ExcelFormulaLabs.DataToolkit
{
    internal static class DictSetCore
    {
        internal static object[,] Frequency(object[] keys)
        { var f=new Dictionary<string,(object k,long c)>(StringComparer.OrdinalIgnoreCase); foreach(var x in keys){string s=ComparisonUtils.SafeKey(x);if(f.TryGetValue(s,out var v))f[s]=(v.k,v.c+1);else f[s]=(x,1);} var r=new object[f.Count,2]; int i=0; foreach(var kv in f){r[i,0]=kv.Value.k;r[i,1]=kv.Value.c;i++;} return r; }

        // review 2026-08-29：DICT.KEYS/VALUES 列提取从 UDF 层下沉（红线① UDF 仅分发）
        internal static object[] Keys(object[,] d)
        {
            var r = new object[d.GetLength(0)];
            for (int i = 0; i < r.Length; i++) r[i] = d[i, 0];
            return r;
        }
        internal static object[] Values(object[,] d)
        {
            var r = new object[d.GetLength(0)];
            for (int i = 0; i < r.Length; i++) r[i] = d[i, 1];
            return r;
        }
        internal static object[] Intersect(object[] a, object[] b) { var sb=new HashSet<string>(b.Select(ComparisonUtils.SafeKey),StringComparer.OrdinalIgnoreCase); var seen=new HashSet<string>(StringComparer.OrdinalIgnoreCase); var r=new List<object>(); foreach(var x in a){if(sb.Contains(ComparisonUtils.SafeKey(x))&&seen.Add(ComparisonUtils.SafeKey(x)))r.Add(x);} return r.ToArray(); }
        internal static object[] Union(object[] a, object[] b) { var s=new HashSet<string>(StringComparer.OrdinalIgnoreCase); var r=new List<object>(); foreach(var x in a.Concat(b)){if(s.Add(ComparisonUtils.SafeKey(x)))r.Add(x);} return r.ToArray(); }
        internal static object[] Except(object[] a, object[] b) { var sb=new HashSet<string>(b.Select(ComparisonUtils.SafeKey),StringComparer.OrdinalIgnoreCase); var seen=new HashSet<string>(StringComparer.OrdinalIgnoreCase); var r=new List<object>(); foreach(var x in a){if(!sb.Contains(ComparisonUtils.SafeKey(x))&&seen.Add(ComparisonUtils.SafeKey(x)))r.Add(x);} return r.ToArray(); }
        internal static object[,] Dict(object[] k, object[] v) { int n=Math.Min(k.Length,v.Length); var r=new object[n,2]; for(int i=0;i<n;i++){r[i,0]=k[i];r[i,1]=v[i];} return r; }
        internal static long Count(object[,] d) => d?.GetLength(0) ?? 0;
    }
}

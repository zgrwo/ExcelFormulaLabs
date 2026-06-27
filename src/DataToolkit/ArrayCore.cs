using System;
using System.Collections.Generic;
using System.Linq;
using FormulaLabs.Foundation;

namespace FormulaLabs.DataToolkit
{
    internal static class ArrayCore
    {
        internal static object[] Sort(object[] a, bool asc = true, string mode = "auto")
        { var m=mode=="numeric"?ComparerMode.Numeric:mode=="text"?ComparerMode.Text:ComparerMode.Auto; var c=new object[a.Length]; Array.Copy(a,c,a.Length); ArrayOperations.Sort(c,asc,m); return c; }
        internal static object[] Unique(object[] a) { var s=new HashSet<string>(); var r=new List<object>(); foreach(var v in a){if(s.Add(ComparisonUtils.SafeKey(v)))r.Add(v);} return r.ToArray(); }
        internal static long IndexOf(object[] a, object v) => ArrayOperations.IndexOf(a, v);
        internal static object[] Slice(object[] a, long start, long len = -1)
        {
            if (start > int.MaxValue || start < int.MinValue)
                throw new ArgumentException($"start_index ({start}) is outside the valid range [{int.MinValue}, {int.MaxValue}].");
            if (len > int.MaxValue || len < int.MinValue)
                throw new ArgumentException($"num_elements ({len}) is outside the valid range [{int.MinValue}, {int.MaxValue}].");
            return ArrayOperations.Slice(a, (int)start, (int)len);
        }
        internal static object[] Flatten2D(object[,] a, string order = "R") => ArrayOperations.Flatten(a, order=="C"?NormalizeOrder.ColumnMajor:NormalizeOrder.RowMajor);
        internal static object[] Filter(object[] a, object crit, string op) { var r=new List<object>(); foreach(var v in a)if(FilterUtils.FilterPasses(v,crit,op))r.Add(v); return r.ToArray(); }
        internal static object[] Concat(object[] a, object[] b) { var r = new object[a.Length + b.Length]; Array.Copy(a, 0, r, 0, a.Length); Array.Copy(b, 0, r, a.Length, b.Length); return r; }
        internal static object[] Reverse(object[] a) { var r=new object[a.Length]; for(int i=0;i<a.Length;i++)r[i]=a[a.Length-1-i]; return r; }
        internal static long Count(object[] a) => a.Length;
        internal static bool Contains(object[] a, object v) => ArrayOperations.IndexOf(a,v)>=0;
        internal static object[] CollectNumeric(object[,] data, int rows, int cols, out string[] names, bool header = true) { var ci=ArrayOperations.CollectNumericColumns(data,rows,cols,out names,header); return ci.Select(i=>(object)(long)i).ToArray(); }

        internal static object[] Shuffle(object[] a)
        {
            var r = new object[a.Length]; Array.Copy(a, r, a.Length);
#if NET8_0_OR_GREATER
            var rng = System.Random.Shared;
#else
            var rng = ThreadLocalRng.Value!;
#endif
            for (int i = r.Length - 1; i > 0; i--) { int j = rng.Next(i + 1); var t = r[i]; r[i] = r[j]; r[j] = t; }
            return r;
        }
#if !NET8_0_OR_GREATER
        private static readonly System.Threading.ThreadLocal<System.Random> ThreadLocalRng = new(() => new System.Random());
#endif
    }
}

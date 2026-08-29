using System;
using System.Collections.Generic;
using System.Linq;
using ExcelFormulaLabs.Foundation;

namespace ExcelFormulaLabs.DataToolkit
{
    internal static class ArrayCore
    {
        internal static object[] Sort(object[] a, bool asc = true, ComparerMode mode = ComparerMode.Auto)
        { var c=new object[a.Length]; Array.Copy(a,c,a.Length); ArrayOperations.Sort(c,asc,mode); return c; }
        internal static object[] Unique(object[] a) { var s=new HashSet<string>(); var r=new List<object>(); foreach(var v in a){if(s.Add(ComparisonUtils.SafeKey(v)))r.Add(v);} return r.ToArray(); }
        internal static long IndexOf(object[] a, object v) => ArrayOperations.IndexOf(a, v);
        internal static object[] Slice(object[] a, long start, long len = -1)
        {
            if (start > int.MaxValue || start < int.MinValue)
                throw new ArgumentException(ErrorMsg.Get("ARR_StartOutOfRange", start, int.MinValue, int.MaxValue));
            if (len > int.MaxValue || len < int.MinValue)
                throw new ArgumentException(ErrorMsg.Get("ARR_LengthOutOfRange", len, int.MinValue, int.MaxValue));
            return ArrayOperations.Slice(a, (int)start, (int)len);
        }
        internal static object[] Flatten2D(object[,] a, string order = "R") => ArrayOperations.Flatten(a, order=="C"?NormalizeOrder.ColumnMajor:NormalizeOrder.RowMajor);
        internal static object[] Filter(object[] a, object crit, string op) { var r=new List<object>(); foreach(var v in a)if(FilterUtils.FilterPasses(v,crit,op))r.Add(v); return r.ToArray(); }
        internal static object[] Concat(object[] a, object[] b) { var r = new object[a.Length + b.Length]; Array.Copy(a, 0, r, 0, a.Length); Array.Copy(b, 0, r, a.Length, b.Length); return r; }
        internal static object[] Reverse(object[] a) { var r=new object[a.Length]; for(int i=0;i<a.Length;i++)r[i]=a[a.Length-1-i]; return r; }
        internal static long Count(object[] a) => a.Length;
        internal static bool Contains(object[] a, object v) => ArrayOperations.IndexOf(a,v)>=0;

        // review-2026-08-29 P1-2：ARR.FILL / ARR.RANGE 业务逻辑从 UDF 层下沉至 Core（UDF 仅分发）。
        internal static object[] Fill(object value, long count)
        {
            if (count < 0 || count > 100_000)
                throw new ArgumentException(ErrorMsg.Get("ARR_CountOutOfRange", count, 100_000));
            var r = new object[count];
            for (int i = 0; i < count; i++) r[i] = value;
            return r;
        }
        internal static object[] Sequence(double start, double end, double step)
        {
            // review 2026-08-29（发行前 max level 复审）：原仅挡 NaN，±Inf 的 start/end/step 未挡，
            // 会静默产生退化序列（如 step=+Inf 返回 [start]）或 `d` 溢出为 Inf 后错误地落入
            // `d > 100_000` 抛错（误导性消息）。与 NaN 一致的哨兵契约：非有限输入 → 空数组。
            if (double.IsNaN(start) || double.IsNaN(end) || double.IsNaN(step)
                || double.IsInfinity(start) || double.IsInfinity(end) || double.IsInfinity(step))
                return Array.Empty<object>();
            if (step == 0)
                throw new ArgumentException(ErrorMsg.Get("ARR_StepZero"));
            bool asc = step > 0;
            if ((asc && start > end) || (!asc && start < end))
                return Array.Empty<object>();
            double d = Math.Abs((end - start) / step);
            // review 2026-08-29（max level 复审）：(int)d 对 d≥2³¹ 或 ±Inf 在 unchecked 下回绕为
            // int.MinValue（如 SEQUENCE(0,1e10,1) 或有限极端值 end-start 溢出为 Inf）→
            // 消息「-2147483648 elements」误导。非有限 d 与超限 d 统一抛错，消息计数用可表示值。
            if (double.IsNaN(d) || double.IsInfinity(d) || d > 100_000)
                throw new ArgumentException(ErrorMsg.Get("ARR_RangeTooLarge",
                    d > 100_000 && d <= int.MaxValue ? (int)d : 100_001, 100_000));
            int n = (int)Math.Floor(d) + 1;
            if (n < 1) n = 1;
            if (n > 100_000)
                throw new ArgumentException(ErrorMsg.Get("ARR_RangeTooLarge", n, 100_000));
            var r = new List<object>(n);
            for (int i = 0; i < n; i++) r.Add(start + i * step);
            return r.ToArray();
        }
        internal static object[] CollectNumeric(object[,] data, int rows, int cols, out string[] names, bool hasHeaders = true) { var ci=ArrayOperations.CollectNumericColumns(data,rows,cols,out names,hasHeaders); return ci.Select(i=>(object)(long)i).ToArray(); }

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
        private static readonly System.Threading.ThreadLocal<System.Random> ThreadLocalRng = new(() => new System.Random(Guid.NewGuid().GetHashCode()));
#endif
    }
}

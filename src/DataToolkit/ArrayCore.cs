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
            // 非有限 start/end/step → 空数组（与 NaN 一致的哨兵契约）：仅挡 NaN 会静默产生
            // 退化序列（如 step=+Inf 返回 [start]），或 `d` 溢出为 Inf 后错误地落入
            // `d > 100_000` 抛错（误导性消息）。
            if (double.IsNaN(start) || double.IsNaN(end) || double.IsNaN(step)
                || double.IsInfinity(start) || double.IsInfinity(end) || double.IsInfinity(step))
                return Array.Empty<object>();
            if (step == 0)
                throw new ArgumentException(ErrorMsg.Get("ARR_StepZero"));
            bool asc = step > 0;
            if ((asc && start > end) || (!asc && start < end))
                return Array.Empty<object>();
            double d = Math.Abs((end - start) / step);
            // (int)d 对 d≥2³¹ 或 ±Inf 在 unchecked 下回绕为 int.MinValue
            // （如 SEQUENCE(0,1e10,1) 或有限极端值 end-start 溢出为 Inf）→ 消息「-2147483648
            // elements」误导。非有限 d 与超限 d 统一抛错，消息计数用可表示值。
            if (double.IsNaN(d) || double.IsInfinity(d) || d > 100_000)
                throw new ArgumentException(ErrorMsg.Get("ARR_RangeTooLarge",
                    d > 100_000 && d <= int.MaxValue ? (int)d : 100_001, 100_000));
            // 浮点步长丢端点——RANGE(0,0.3,0.1) 的 d=2.9999999999999996 → floor 得 2 → 缺 0.3。
            // 计数容差在相对 eps 基础上并入 start 的 ulp 粒度：start 很大时 `end - start`
            // 灾难性抵消（1e8+0.3-1e8=0.2999999821），d 的偏差可达 ulp(start)/|step| 量级，
            // 仅 1e-12 相对容差补不回端点。上限 0.1 防极端 start/step 比把非整数 d 误判为整数。
            double countTol = Math.Max(1e-12 * Math.Max(1.0, Math.Abs(d)),
                Math.Min(4.0 * (Math.Abs(start) * 2.220446049250313e-16) / Math.Abs(step), 0.1));
            bool landsOnEnd = Math.Abs(d - Math.Round(d)) <= countTol;
            int n = (int)Math.Floor(d + countTol) + 1;
            if (n < 1) n = 1;
            if (n > 100_000)
                throw new ArgumentException(ErrorMsg.Get("ARR_RangeTooLarge", n, 100_000));
            // 端点吸附仅限「数学上可达」（landsOnEnd，末项用精确 end）或纯浮点噪声
            // （数个 ulp 内残差）。旧的 1e-9·max(|start|,|end|,|step|) 容差在 1.7e9 量级
            // 达 1.7——0.7 步长下离端点 0.6 的**真实**末项 1700000099.4 被错误吸附到
            // 1700000100（R1-5），同时把 1e8 量的真实端点（差 0.1）当超端剔除。
            double endUlpTol = 4.0 * 2.220446049250313e-16
                * Math.Max(Math.Max(Math.Abs(start), Math.Abs(end)), Math.Abs(step));
            var r = new List<object>(n);
            for (int i = 0; i < n; i++)
            {
                double v = start + i * step;
                if (i == n - 1)
                {
                    if (landsOnEnd) v = end;                                        // 端点数学可达 → 精确 end
                    else if (Math.Abs(v - end) <= endUlpTol) v = end;               // 仅纯浮点噪声内吸附
                    else if (asc ? v > end : v < end) continue;                     // 超端项剔除
                }
                r.Add(v);
            }
            return r.ToArray();
        }
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

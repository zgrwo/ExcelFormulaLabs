using System;

namespace ExcelFormulaLabs.Foundation
{
    /// <summary>
    /// 单次 UDF 调用的数组级 Regex 墙钟预算（R1-4）。
    /// 每格独立 5s Timeout 在 MapOver 数组分发下线性放大（N 格 = N×5s CPU，
    /// 单格超时异常被 per-cell 隔离后继续下一格）；本预算把一次调用的正则计算
    /// 总时长钳在 <see cref="PerCall"/> 内：UDF 入口 <see cref="Begin"/> 建立作用域，
    /// 正则操作取 min(单次 5s, 剩余额度) 作为有效 Timeout，耗尽即抛
    /// <see cref="TimeoutException"/> → WrapError → #VALUE!（后续格子立即失败，不再累计）。
    /// ThreadStatic：Excel 计算线程内独立，无跨线程竞争。
    /// </summary>
    public static class RegexBudget
    {
        /// <summary>单次 UDF 调用允许的 Regex 计算总预算（与单次操作 Timeout 同量级）。</summary>
        public static readonly TimeSpan PerCall = TimeSpan.FromSeconds(5);

        [ThreadStatic] private static long _deadline;
        [ThreadStatic] private static int _depth;

        /// <summary>进入预算作用域（可嵌套；仅最外层设置/清除 deadline）。</summary>
        public static IDisposable Begin()
        {
            if (++_depth == 1)
                _deadline = System.Diagnostics.Stopwatch.GetTimestamp()
                    + (long)(PerCall.TotalSeconds * System.Diagnostics.Stopwatch.Frequency);
            return new Scope();
        }

        /// <summary>剩余预算；无作用域（直接 Core 调用/单测）时返回 <see cref="PerCall"/>，
        /// 保持既有单次调用语义。</summary>
        public static TimeSpan Remaining
        {
            get
            {
                if (_depth == 0) return PerCall;
                long left = _deadline - System.Diagnostics.Stopwatch.GetTimestamp();
                if (left <= 0) return TimeSpan.Zero;
                return TimeSpan.FromSeconds((double)left / System.Diagnostics.Stopwatch.Frequency);
            }
        }

        /// <summary>预算耗尽 → 抛 <see cref="TimeoutException"/>（WrapError 落地 #VALUE!）。</summary>
        public static void ThrowIfExhausted(string op)
        {
            if (Remaining <= TimeSpan.Zero)
                throw new TimeoutException(
                    $"{op} exceeded the per-call 5-second regex budget — " +
                    "possible catastrophic backtracking (ReDoS). Narrow the input range.");
        }

        private sealed class Scope : IDisposable
        {
            private bool _done;
            public void Dispose()
            {
                if (_done) return;
                _done = true;
                if (--_depth == 0) _deadline = 0;
            }
        }
    }
}

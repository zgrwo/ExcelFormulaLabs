using System;

namespace ExcelFormulaLabs.Foundation
{
    /// <summary>
    /// Centralized exception filter policy.
    /// 捕获后**吞掉或转换**的 <c>catch</c> 块必须使用 <see cref="IsCatchable"/> 作为
    /// <c>when</c> 守卫，确保进程级致命异常永不被吞没。
    /// </summary>
    /// <remarks>
    /// Excluded (re-thrown) exceptions:
    /// <list type="bullet">
    ///   <item><see cref="OutOfMemoryException"/> — process cannot recover.</item>
    ///   <item><see cref="StackOverflowException"/> — process cannot recover.</item>
    ///   <item><see cref="AccessViolationException"/> — corrupted state (CLR 4+).</item>
    /// </list>
    /// 例外（F-01，review-2026-09-24）：捕获**具体非致命类型**并立即 rethrow 或
    /// 跳过候选后继续（如捕获 ArgumentException 后置 skipped 标志、捕获
    /// RegexMatchTimeoutException 后直接 rethrow）无需 <c>when</c> 守卫——
    /// 这些类型在继承层次上不可能是致命异常，守卫只是冗余；无过滤的空捕获块
    /// 仍由 pre-commit 检查 1 强制禁止。若未来识别出新的致命异常类型，在本类集中
    /// 登记——一处修改传播到全部 25+ 捕获点。
    /// </remarks>
    public static class ExceptionFilters
    {
        /// <summary>
        /// Returns <c>true</c> if <paramref name="ex"/> is safe to catch and handle.
        /// Returns <c>false</c> for process-fatal exceptions that must propagate.
        /// </summary>
        /// <param name="ex">The exception being evaluated.</param>
        /// <returns><c>true</c> → catch and handle; <c>false</c> → re-throw.</returns>
        public static bool IsCatchable(Exception ex)
            => ex is not OutOfMemoryException
               and not StackOverflowException
               and not AccessViolationException;
    }
}

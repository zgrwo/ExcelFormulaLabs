using System;

namespace ExcelFormulaLabs.Foundation
{
    /// <summary>
    /// Centralized exception filter policy.
    /// All <c>catch</c> blocks in the codebase MUST use <see cref="IsCatchable"/>
    /// as the <c>when</c> guard so that process-fatal exceptions are never swallowed.
    /// </summary>
    /// <remarks>
    /// Excluded (re-thrown) exceptions:
    /// <list type="bullet">
    ///   <item><see cref="OutOfMemoryException"/> — process cannot recover.</item>
    ///   <item><see cref="StackOverflowException"/> — process cannot recover.</item>
    ///   <item><see cref="AccessViolationException"/> — corrupted state (CLR 4+).</item>
    /// </list>
    /// If additional fatal exception types are identified in the future,
    /// they are added HERE — one change propagates to all 25+ catch sites.
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

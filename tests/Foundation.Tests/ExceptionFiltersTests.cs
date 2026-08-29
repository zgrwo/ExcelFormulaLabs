using System;
using ExcelFormulaLabs.Foundation;
using FluentAssertions;
using Xunit;

namespace ExcelFormulaLabs.Foundation.Tests
{
    /// <summary>review-2026-08-29 P2-5：ExceptionFilters 此前无专用测试。
    /// 验证进程致命异常（OOM/StackOverflow/AccessViolation）永远不被 catch 吞掉。</summary>
    public class ExceptionFiltersTests
    {
        [Fact] public void IsCatchable_ordinary_exception_true()
        {
            ExceptionFilters.IsCatchable(new InvalidOperationException()).Should().BeTrue();
            ExceptionFilters.IsCatchable(new ArgumentException("x")).Should().BeTrue();
            ExceptionFilters.IsCatchable(new DivideByZeroException()).Should().BeTrue();
            ExceptionFilters.IsCatchable(new InvalidCastException()).Should().BeTrue();
        }

        [Fact] public void IsCatchable_out_of_memory_false()
        {
            ExceptionFilters.IsCatchable(new OutOfMemoryException()).Should().BeFalse();
        }

        [Fact] public void IsCatchable_stack_overflow_false()
        {
            ExceptionFilters.IsCatchable(new StackOverflowException()).Should().BeFalse();
        }

        [Fact] public void IsCatchable_access_violation_false()
        {
            ExceptionFilters.IsCatchable(new AccessViolationException()).Should().BeFalse();
        }

        [Fact] public void IsCatchable_null_returns_true()
        {
            // null 不是致命异常类型 → is not 模式短路返回 true（catch 过滤中 ex 不会为 null，
            // 此断言文档化当前行为而非假定抛 NRE）
            ExceptionFilters.IsCatchable(null!).Should().BeTrue();
        }

        [Fact] public void IsCatchable_derived_fatal_types_false()
        {
            // 派生类型同样致命（InsufficientMemoryException : OutOfMemoryException）
            ExceptionFilters.IsCatchable(new InsufficientMemoryException()).Should().BeFalse();
        }
    }
}

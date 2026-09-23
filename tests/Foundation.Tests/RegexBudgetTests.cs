using System;
using System.Reflection;
using ExcelFormulaLabs.Foundation;
using FluentAssertions;
using Xunit;

public class RegexBudgetTests
{
    [Fact] public void Remaining_without_scope_equals_per_call()
        => RegexBudget.Remaining.Should().Be(RegexBudget.PerCall);

    [Fact] public void Begin_sets_scope_and_dispose_restores_baseline()
    {
        using (RegexBudget.Begin())
        {
            RegexBudget.Remaining.Should().BeGreaterThan(TimeSpan.Zero);
            RegexBudget.Remaining.Should().BeLessThanOrEqualTo(RegexBudget.PerCall);
        }
        RegexBudget.Remaining.Should().Be(RegexBudget.PerCall);
    }

    [Fact] public void Nested_scopes_restore_baseline_after_both_disposed()
    {
        var outer = RegexBudget.Begin();
        var inner = RegexBudget.Begin();
        inner.Dispose();
        RegexBudget.Remaining.Should().BeLessThanOrEqualTo(RegexBudget.PerCall);
        outer.Dispose();
        RegexBudget.Remaining.Should().Be(RegexBudget.PerCall);
    }

    [Fact] public void Scope_dispose_is_idempotent()
    {
        var scope = RegexBudget.Begin();
        scope.Dispose();
        scope.Dispose();
        RegexBudget.Remaining.Should().Be(RegexBudget.PerCall);
    }

    [Fact] public void ThrowIfExhausted_throws_TimeoutException_when_deadline_passed()
    {
        // 反射拨动 ThreadStatic 私有 deadline（仅测试用），避免真实等待 5 秒。
        var deadline = typeof(RegexBudget).GetField("_deadline", BindingFlags.NonPublic | BindingFlags.Static)!;
        var depth = typeof(RegexBudget).GetField("_depth", BindingFlags.NonPublic | BindingFlags.Static)!;
        try
        {
            depth.SetValue(null, 1);
            deadline.SetValue(null, System.Diagnostics.Stopwatch.GetTimestamp() - 1);
            RegexBudget.Remaining.Should().Be(TimeSpan.Zero);
            Action act = () => RegexBudget.ThrowIfExhausted("unit-test");
            act.Should().Throw<TimeoutException>().WithMessage("*budget*");
        }
        finally
        {
            deadline.SetValue(null, 0L);
            depth.SetValue(null, 0);
        }
    }

    [Fact] public void ThrowIfExhausted_with_fresh_scope_does_not_throw()
    {
        using (RegexBudget.Begin())
        {
            Action act = () => RegexBudget.ThrowIfExhausted("unit-test");
            act.Should().NotThrow();
        }
    }
}

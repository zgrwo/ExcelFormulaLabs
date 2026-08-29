using ExcelFormulaLabs.Analytics;
using ExcelFormulaLabs.Foundation;
using FluentAssertions;
using Xunit;

namespace ExcelFormulaLabs.Analytics.Tests
{
    public class DoeUdfTests
    {
        [Fact] public void Plan_returns_matrix()
        {
            var r = (object[,])DoeUdf.UDF_DOE_PLAN(2, 2, 0, 2, "full", false, null!);
            r.GetLength(0).Should().Be(5);
            r.GetLength(1).Should().Be(4);
            r[0, 0].Should().Be("StdOrder");
        }

        [Fact] public void Plan_default_randomize_true_with_seed_is_deterministic()
        {
            var a = (object[,])DoeUdf.UDF_DOE_PLAN(2, 2, 0, 2, "full", null!, 7);
            var b = (object[,])DoeUdf.UDF_DOE_PLAN(2, 2, 0, 2, "full", null!, 7);
            for (int i = 1; i <= 4; i++)
                a[i, 1].Should().Be(b[i, 1]);
        }

        [Fact] public void Plan_zero_level_returns_error()
            => DoeUdf.UDF_DOE_PLAN(1, 0, 0, 2, "full", false, null!).Should().Be(ExcelError.Value);

        [Fact] public void Plan_no_factors_returns_error()
            => DoeUdf.UDF_DOE_PLAN(0, 2, 0, 2, "full", false, null!).Should().Be(ExcelError.Value);

        [Fact] public void Plan_unknown_method_returns_error()
            => DoeUdf.UDF_DOE_PLAN(1, 2, 0, 2, "nope", false, null!).Should().Be(ExcelError.Value);

        // review 2026-08-29：DOE 上限守卫（位移回绕 + cells）UDF 级验证——84 因子 FRAC 原会分配 352MB+，
        // 修复后应返回 #VALUE! 而非 OOM 崩溃。
        [Fact] public void Plan_fractional_shift_wrap_returns_error()
            => DoeUdf.UDF_DOE_PLAN(84, 2, 0, 2, "fractional", false, null!).Should().Be(ExcelError.Value);

        [Fact] public void Plan_bb_cells_guard_returns_error()
            => DoeUdf.UDF_DOE_PLAN(700, 2, 0, 2, "bb", false, null!).Should().Be(ExcelError.Value);

        // review 2026-08-29（发行前 max level 复审）：超因子数在按因子分配数组前抛异常 → UDF 返回 #VALUE!
        // 而非 32 位 Excel OOM 崩溃。此前 =DOE.PLAN(1000000000,2,0,1,"FULL") 会尝试 4GB 分配；
        // 测试用 MaxFactors+1 避免回归时真实 4GB 分配。
        [Fact] public void Plan_huge_factor_count_returns_error()
            => DoeUdf.UDF_DOE_PLAN(DoeCore.MaxFactors + 1, 2, 0, 2, "full", false, null!).Should().Be(ExcelError.Value);
    }
}

// 评审复核复现集（review-verification repro）。
// 命名规则：<评审编号>_<断言要点>。每条期望值均来自可复现的外部 oracle，
// 注释里给出 oracle 与算式（禁止用被测实现生成期望值）。
// 复核通过后，本文件中的用例将按其归属并入 {Module}CoreTests.cs 作为回归守卫。
using System;
using System.Reflection;
using ExcelFormulaLabs.Analytics;
using FluentAssertions;
using Xunit;

namespace ExcelFormulaLabs.Analytics.Tests
{
    public class ReviewVerificationTests
    {
        // ─────────────────────────────────────────────────────────────
        // A1 — LINALG.DET 缺少分解族通用的 MaxAbs 尺度归一化
        // oracle: numpy.linalg.det(np.diag([1e300,1e300,1e-300,1e-300])) == 1.0
        //         真值 = 1e300*1e300*1e-300*1e-300 = 1（精确）
        // ─────────────────────────────────────────────────────────────

        private static double[,] WideScaleDiag(bool reversed = false)
        {
            var m = new double[4, 4];
            double[] d = reversed
                ? new[] { 1e-300, 1e-300, 1e300, 1e300 }
                : new[] { 1e300, 1e300, 1e-300, 1e-300 };
            for (int i = 0; i < 4; i++) m[i, i] = d[i];
            return m;
        }

        [Fact]
        public void A1_Det_wideScaleDiagonal_shouldBeOne()
            => LinalgCore.Determinant(WideScaleDiag())
                .Should().BeApproximately(1.0, 1e-9, "numpy: det(diag(1e300,1e300,1e-300,1e-300)) == 1.0");

        [Fact]
        public void A1b_Det_mustNotDependOnDiagonalOrder()
            => LinalgCore.Determinant(WideScaleDiag(reversed: true))
                .Should().BeApproximately(1.0, 1e-9, "行列式与对角顺序无关，numpy 两序皆为 1.0");

        [Fact] // 对照：常规量纲必须仍然正确（防止修复引入回归）
        public void A1c_Det_moderateScale_control()
            => LinalgCore.Determinant(new double[,] { { 2, 0, 0, 0 }, { 0, 3, 0, 0 }, { 0, 0, 4, 0 }, { 0, 0, 0, 5 } })
                .Should().BeApproximately(120.0, 1e-12);

        // ─────────────────────────────────────────────────────────────
        // A2 — FitOLS(addIntercept:false) 的 R² 用了中心化 TSS
        // oracle: statsmodels.OLS(y, X).fit().rsquared  (无常数项 → 非中心化 TSS)
        //         X=[[1],[2],[3]], y=[1,2,4] → 0.9829931972789115
        //         手算: β=17/14, SSE=0.3571428…, 非中心化 TSS=Σy²=21 → 1−SSE/21
        //         中心化 TSS=4.6666… → 1−SSE/4.6666… = 0.923469387755102（= 当前实现值）
        // ─────────────────────────────────────────────────────────────

        [Fact]
        public void A2_OLS_withoutIntercept_r2_matchesStatsmodelsDefinition()
        {
            var X = new double[,] { { 1 }, { 2 }, { 3 } };
            var y = new[] { 1.0, 2.0, 4.0 };
            var r2 = (double)RegressionCore.FitOLS(X, y, addIntercept: false)["r_squared"];
            r2.Should().BeApproximately(0.9829931972789115, 1e-12,
                "无截距模型的标准 R² 用非中心化 TSS（statsmodels/R 口径）");
        }

        [Fact] // 对照：含截距必须仍等于 statsmodels 0.9642857142857143
        public void A2b_OLS_withIntercept_r2_control()
        {
            var X = new double[,] { { 1 }, { 2 }, { 3 } };
            var y = new[] { 1.0, 2.0, 4.0 };
            ((double)RegressionCore.FitOLS(X, y)["r_squared"])
                .Should().BeApproximately(0.9642857142857143, 1e-12);
        }

        // ─────────────────────────────────────────────────────────────
        // A3 — FitRidge 的 "df" 键与 FitOLS 语义相反，且同表输出
        // 实测（本机）：n=5、2 个预测列、均 addIntercept=true →
        //   FitOLS["df"]  = n − p = 5 − 3 = 2   （残差自由度，RegressionCore.cs:185）
        //   FitRidge["df"] = p     = 3          （参数个数，RegressionCore.cs:405）
        // 两者都经 AnalyticsHelpers.DictToReport 进同一张 REGRESS 报表 → 用户据 RIDGE
        // 报表推断自由度必然错。期望：同一键同语义（残差自由度）。
        // ─────────────────────────────────────────────────────────────

        [Fact]
        public void A3_Ridge_df_mustMatchOLSResidualDf()
        {
            var X = new double[5, 2];
            for (int i = 0; i < 5; i++) { X[i, 0] = i + 1; X[i, 1] = (i + 1) * (i + 1) * 0.1; }
            var y = new[] { 1.0, 2.1, 2.9, 4.2, 5.1 };
            long olsDf = (long)RegressionCore.FitOLS(X, y)["df"];
            long ridgeDf = (long)RegressionCore.FitRidge(X, y, 0.1)["df"];
            olsDf.Should().Be(2, "n=5，含截距 p=3 → 残差自由度 n−p=2（对照当前 OLS 实现）");
            ridgeDf.Should().Be(olsDf, "同一报表的 df 键必须同语义（残差自由度），不得一处给 p 一处给 n−p");
        }

        // ─────────────────────────────────────────────────────────────
        // A4 — AnovaOneWay 用 LINQ Average()/Math.Pow，合法有限大值溢出
        // 参照：scipy.stats.f_oneway 在 1e308 量纲同样返回 NaN（本库至少显式报错，
        //       优于静默 NaN）；但 ANOVA 是尺度不变量，库内 StatsCore.Mean/Sum 已有
        //       缩放回退，此处口径分裂。本用例记录"尺度不变性"这一理想口径。
        // ─────────────────────────────────────────────────────────────

        [Fact]
        public void A4_Anova_shouldBeScaleInvariant_forFiniteLargeInput()
        {
            var big = new[] { new[] { 5e307, 1e308, 1.5e308 }, new[] { 6e307, 1.1e308, 1.6e308 } };
            var small = new[] { new[] { 5.0, 10.0, 15.0 }, new[] { 6.0, 11.0, 16.0 } };
            double fSmall = (double)RegressionCore.AnovaOneWay(small)["f_stat"];
            double fBig = (double)RegressionCore.AnovaOneWay(big)["f_stat"];
            fBig.Should().BeApproximately(fSmall, 1e-9, "ANOVA 的 F 对公共正缩放不变（scipy 缩小后 F=0.06）");
        }

        // ─────────────────────────────────────────────────────────────
        // A5 — SolveCore.Median 偶数长度用 0.5*(lo+hi)，±MaxValue 溢出为 Inf
        // 对照：StatsCore.Median 走 QuantileCapped/QuantileSafe 凸组合回退（本库已有正解）
        // 注：numpy.median 同样溢出，故这是"库内口径不一致 + 下游误导性报错"问题
        // ─────────────────────────────────────────────────────────────

        [Fact]
        public void A5_SolveCore_median_mustNotOverflow()
        {
            var mi = typeof(SolveCore).GetMethod("Median", BindingFlags.NonPublic | BindingFlags.Static);
            mi.Should().NotBeNull("SolveCore.Median 为 private static");
            object? boxed = mi!.Invoke(null, new object[] { new[] { double.MaxValue, double.MaxValue } });
            boxed.Should().NotBeNull();
            ((double)boxed!).Should().NotBe(double.PositiveInfinity,
                "中位数不应因 0.5*(a+b) 中间溢出而变成 Inf");
        }

        // ─────────────────────────────────────────────────────────────
        // E2 — 求和精度一库两制：PivotCore 用 Neumaier，StatsCore 用朴素折叠
        // oracle: math.fsum([1e16, 1, -1e16]) == 1.0
        // ─────────────────────────────────────────────────────────────

        [Fact]
        public void E2_StatsSum_shouldUseCompensatedSummation()
            => StatsCore.Sum(new[] { 1e16, 1.0, -1e16 })
                .Should().Be(1.0, "math.fsum 给 1.0；Neumaier 补偿求和可消除该量级抵消");

        // ─────────────────────────────────────────────────────────────
        // C1 — SOLVE 的 rng 建在请求行循环之外（SolveCore.cs:1194 在 :1195 的 for 之前），
        //      理论上跨请求行前进 → 相同的请求行可能得到不同推荐值。
        // 复核结论（见评审记录）：**代码层确认，但 7 种夹具（linear/poly × 10/50 起点 ×
        //      可达/不可达/超范围目标）下两行均逐位相同**——LocalSearch 是有界确定性模式
        //      搜索，收敛点与起点无关；可达性包络的两端被 s=0/1 钉死。
        //      故本用例保留为"位置无关性"回归守卫（当前即通过），不作为失败复现。
        // ─────────────────────────────────────────────────────────────

        private static object[,] SolveHistory()
        {
            var d = new object[6, 3];
            d[0, 0] = "IncomingA"; d[0, 1] = "VariableX"; d[0, 2] = "OutputY";
            for (int i = 1; i <= 5; i++)
            {
                d[i, 0] = 1.0;
                d[i, 1] = (double)i;
                d[i, 2] = 2.0 * i + 1.0;
            }
            return d;
        }

        // 目标 y=9 落在历史可达区间 [3,11] 内 → 真解 x=4（对照夹具正确性，避免"两行同样地错"）
        private static object[,] SolveTwoIdenticalRequests()
        {
            var r = new object[3, 3];
            r[0, 0] = "IncomingA"; r[0, 1] = "VariableX"; r[0, 2] = "OutputY";
            for (int i = 1; i <= 2; i++) { r[i, 0] = 1.0; r[i, 1] = null!; r[i, 2] = 9.0; }
            return r;
        }

        [Fact]
        public void C1_Solve_identicalRequestRows_mustReturnIdenticalRecommendations()
        {
            var table = SolveCore.Inverse(SolveHistory(), SolveTwoIdenticalRequests(), null, "linear", 42L, 10);
            table.GetLength(0).Should().Be(3, "表头 + 2 个请求行");
            for (int c = 1; c < table.GetLength(1); c++) // 列 0 是"请求行"标签，本就不同
                table[1, c].Should().Be(table[2, c],
                    $"两个完全相同的请求行在第 {c} 列必须给出相同结果（结果不得依赖行位置）");
        }

        [Fact]
        public void C1b_Solve_identicalRequests_hitExactSolution()
        {
            var table = SolveCore.Inverse(SolveHistory(), SolveTwoIdenticalRequests(), null, "linear", 42L, 10);
            ((double)table[1, 1]).Should().BeApproximately(4.0, 1e-6, "y=9 在 y=2x+1 上的真解为 x=4");
        }
    }
}

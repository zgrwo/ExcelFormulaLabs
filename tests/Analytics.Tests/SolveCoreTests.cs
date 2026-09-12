using System;
using System.Collections.Generic;
using System.Linq;
using ExcelFormulaLabs.Analytics;
using FluentAssertions;
using Xunit;

namespace ExcelFormulaLabs.Analytics.Tests
{
    public class SolveCoreTests
    {
        // y = 2 + 0.5·a + 1.5·u with a = 1..10 and deliberately non-collinear u
        // (u = 2a would make the design rank-deficient and FitOLS must reject that).
        private static double UOf(int a) => ((a * 3) % 10) + 2.0;
        private static double YOf(double a, double u) => 2.0 + 0.5 * a + 1.5 * u;
        private static readonly double[][] XLin = Enumerable.Range(1, 10)
            .Select(i => new[] { (double)i, UOf(i) }).ToArray();
        private static readonly double[] YLin = XLin.Select(r => YOf(r[0], r[1])).ToArray();

        private static object[,] LinearTable(bool withRequestRow)
        {
            int rows = withRequestRow ? 12 : 11;
            var t = new object[rows, 3];
            t[0, 0] = "IncomingA"; t[0, 1] = "VariableU1"; t[0, 2] = "OutputY1";
            for (int i = 0; i < 10; i++)
            {
                double a = i + 1;
                t[i + 1, 0] = a;
                t[i + 1, 1] = UOf(i + 1);
                t[i + 1, 2] = YOf(a, UOf(i + 1));
            }
            if (withRequestRow)
            {
                t[11, 0] = 10.0;
                t[11, 1] = null!;
                t[11, 2] = 13.0;
            }
            return t;
        }

        private static double[][] FitAutoFixture(int n, int k, out double[] y)
        {
            var X = new double[n][];
            y = new double[n];
            ulong state = 88172645463325252UL;
            double Next()
            {
                state ^= state << 13;
                state ^= state >> 7;
                state ^= state << 17;
                return (state % 100000) / 100000.0;
            }
            for (int i = 0; i < n; i++)
            {
                X[i] = new double[k];
                double sum = 0;
                for (int j = 0; j < k; j++) { X[i][j] = Next(); sum += X[i][j]; }
                y[i] = 1.0 + sum;
            }
            return X;
        }

        // ──────────────────────────── Task 1.1 表解析 ────────────────────────────

        [Fact]
        public void ParseSchema_ClassifiesRowsAndRoles()
        {
            object[,] data = {
                {"IncomingA","VariableU1","OutputY1","备注"},
                {1.0, 2.0, 9.5, "x"},
                {10.0, null!, 13.0, null!},
            };
            var s = SolveCore.ParseSchema(data);
            s.Incoming.Should().Equal(0);
            s.Variable.Should().Equal(1);
            s.Output.Should().Equal(2);
            s.Features.Should().Equal(0, 1);
            s.HistoryRows.Should().Equal(0);
            s.RequestRows.Should().Equal(1);
        }

        [Fact]
        public void ParseSchema_ChinesePrefixes()
        {
            object[,] data = {
                {"来料温度","可调压力","固定时间","输出收率"},
                {100.0, 2.0, 60.0, 0.9},
            };
            var s = SolveCore.ParseSchema(data);
            s.Incoming.Should().Equal(0);
            s.Variable.Should().Equal(1);
            s.Fixed.Should().Equal(2);
            s.Output.Should().Equal(3);
            s.HistoryRows.Should().Equal(0);
        }

        [Fact]
        public void ParseSchema_NoVariableColumn_throws()
        {
            object[,] data = { { "IncomingA", "OutputY1" }, { 1.0, 2.0 } };
            var act = () => SolveCore.ParseSchema(data);
            act.Should().Throw<ArgumentException>().WithMessage("*Variable*");
        }

        [Fact]
        public void ParseSchema_NoOutputColumn_throws()
        {
            object[,] data = { { "IncomingA", "VariableU1" }, { 1.0, 2.0 } };
            var act = () => SolveCore.ParseSchema(data);
            act.Should().Throw<ArgumentException>().WithMessage("*Output*");
        }

        [Fact]
        public void ParseSchema_RequestRowIncomingBlank_throws()
        {
            object[,] data = {
                {"IncomingA","VariableU1","OutputY1"},
                {null!, null!, 13.0},
            };
            var act = () => SolveCore.ParseSchema(data);
            act.Should().Throw<ArgumentException>().WithMessage("*Incoming*blank*");
        }

        [Fact]
        public void ParseSchema_IgnoresUnrecognizedColumnsAndBlankRows()
        {
            object[,] data = {
                {"Batch","IncomingA","VariableU1","OutputY1"},
                {"B1", 1.0, 2.0, 9.5},
                {null!, null!, null!, null!},
                {"B2", 10.0, null!, 13.0},
            };
            var s = SolveCore.ParseSchema(data);
            s.Features.Should().Equal(1, 2);
            s.HistoryRows.Should().Equal(0);
            s.RequestRows.Should().Equal(2);
        }

        // ──────────────────────────── Task 1.2 拟合与预测 ────────────────────────────

        [Fact]
        public void FitModel_Linear_ExactData()
        {
            var m = SolveCore.FitModel(XLin, YLin, "linear");
            m.Kind.Should().Be("linear");
            m.Powers.Length.Should().Be(2);
            m.Coef[0].Should().BeApproximately(0.5, 1e-9);
            m.Coef[1].Should().BeApproximately(1.5, 1e-9);
            m.Intercept.Should().BeApproximately(2.0, 1e-9);
            SolveCore.Predict(m, new[] { 10.0, 4.0 }).Should().BeApproximately(13.0, 1e-10);
            for (int i = 0; i < XLin.Length; i++)
                SolveCore.Predict(m, XLin[i]).Should().BeApproximately(YLin[i], 1e-10);
        }

        [Fact]
        public void FitModel_Poly_ExactQuadratic()
        {
            // y = 1 + u + 0.5u² for u = -3..3. Poly uses ridge λ=1e-5 on standardized terms,
            // so the de-standardized coefficients carry a ~λ/n relative shrinkage; prediction
            // tolerance 1e-4 reflects that budget (linear branch is exact to 1e-10).
            var X = Enumerable.Range(-3, 7).Select(i => new[] { (double)i }).ToArray();
            var y = X.Select(r => 1.0 + r[0] + 0.5 * r[0] * r[0]).ToArray();
            var m = SolveCore.FitModel(X, y, "poly");
            m.Kind.Should().Be("poly");
            m.Powers.Length.Should().Be(2);
            foreach (var row in X)
                SolveCore.Predict(m, row).Should().BeApproximately(1.0 + row[0] + 0.5 * row[0] * row[0], 1e-4);
        }

        [Fact]
        public void ExpandedTermCount_Formulas()
        {
            SolveCore.ExpandedTermCount(11, "linear").Should().Be(11);
            SolveCore.ExpandedTermCount(11, "poly").Should().Be(77);
            SolveCore.ExpandedTermCount(15, "poly").Should().Be(135);
            SolveCore.ExpandedTermCount(1, "poly").Should().Be(2);
        }

        [Fact]
        public void FitModel_Poly_TermLimit_throws()
        {
            var X = FitAutoFixture(20, 15, out var y);
            var act = () => SolveCore.FitModel(X, y, "poly");
            act.Should().Throw<ArgumentException>().WithMessage("*exceeds the limit*");
        }

        [Fact]
        public void FitModel_NotEnoughRows_throws()
        {
            var X = Enumerable.Range(1, 5).Select(i => new[] { (double)i, (double)(i * i) }).ToArray();
            var y = X.Select(r => r[0] + r[1]).ToArray();
            var act = () => SolveCore.FitModel(X, y, "poly"); // 5 terms need ≥6 rows
            act.Should().Throw<ArgumentException>().WithMessage("*Not enough history rows*");
        }

        [Fact]
        public void FitModel_UnknownModel_throws()
        {
            var act = () => SolveCore.FitModel(XLin, YLin, "gpr");
            act.Should().Throw<ArgumentException>().WithMessage("*Unknown model*gpr*");
        }

        // ──────────────────────────── Task 1.3 交叉验证与 auto ────────────────────────────

        [Fact]
        public void CrossValidate_FiveFold_ExactLinear()
        {
            int n = 25;
            var X = Enumerable.Range(1, n).Select(i => new[] { (double)i, UOf(i) }).ToArray();
            var y = X.Select(r => YOf(r[0], r[1])).ToArray();
            var (scheme, r2, mae) = SolveCore.CrossValidate(X, y, "linear", 42L);
            scheme.Should().Be("5折");
            r2.Should().BeGreaterThanOrEqualTo(0.999);
            mae.Should().BeLessThanOrEqualTo(1e-6);
        }

        [Fact]
        public void CrossValidate_LooForSmallSample()
        {
            var (scheme, r2, _) = SolveCore.CrossValidate(XLin, YLin, "linear", 42L);
            scheme.Should().Be("LOO");
            r2.Should().BeGreaterThanOrEqualTo(0.999);
        }

        [Fact]
        public void CrossValidate_TooFewRows_throws()
        {
            var X = new[] { new[] { 1.0 }, new[] { 2.0 }, new[] { 3.0 }, new[] { 4.0 } };
            var y = new[] { 2.0, 4.0, 6.0, 8.0 };
            var act = () => SolveCore.CrossValidate(X, y, "linear", 42L);
            act.Should().Throw<ArgumentException>().WithMessage("*at least 5*");
        }

        [Fact]
        public void FitAuto_PolySkipped_SmallSample()
        {
            var X = FitAutoFixture(33, 11, out var y);
            var (model, chosen, _, r2, _, polySkipped, rateSkipped) = SolveCore.FitAuto(X, y, 42L);
            polySkipped.Should().BeTrue();
            rateSkipped.Should().BeTrue(); // 无 Time 列/配对 → rate 结构不可用
            chosen.Should().Be("linear");
            model.Kind.Should().Be("linear");
            r2.Should().BeGreaterThanOrEqualTo(0.99);
        }

        // ──────────────────────────── Task 1.4 反解与可达性 ────────────────────────────

        private static double[,] SolveLinear(double target, double init = double.NaN, long seed = 42L)
        {
            var Y = YLin.Select(v => new[] { v }).ToArray();
            return SolveCore.SolveInverse(
                XLin, Y, new[] { 1 },
                new[] { new[] { 10.0, init } },
                new[] { new[] { target } },
                new[] { new[] { 2.0, 20.0 } },
                "linear", seed, 10);
        }

        [Fact]
        public void SolveInverse_LinearFixture_HitsTarget()
        {
            var r = SolveLinear(13.0);
            r.GetLength(0).Should().Be(1);
            r.GetLength(1).Should().Be(4); // v=1, m=1 → rec, pred, σ, status
            r[0, 0].Should().BeApproximately(4.0, 1e-4);
            r[0, 1].Should().BeApproximately(13.0, 1e-6);
            r[0, 3].Should().Be(0.0); // 0 = 可达
        }

        [Fact]
        public void SolveInverse_InitialValueConvergesToSameSolution()
        {
            var r = SolveLinear(13.0, init: 3.99);
            r[0, 0].Should().BeApproximately(4.0, 1e-4);
            r[0, 1].Should().BeApproximately(13.0, 1e-6);
            r[0, 3].Should().Be(0.0);
        }

        [Fact]
        public void SolveInverse_UnreachableTarget_StatusUnreachable()
        {
            var r = SolveLinear(1000.0);
            r[0, 3].Should().Be(1.0); // 1 = 不可达
        }

        [Fact]
        public void SolveInverse_MicroScale_ReachabilityIsRelative()
        {
            // R-2 regression: y ~ 1e-12, target 50% above the reachable maximum must be
            // unreachable. An absolute epsilon (~1e-15) would be 0.1% of this data scale
            // and could silently absorb the gap; the relative tolerance must not.
            var X = new[] { new[] { 0.0 }, new[] { 0.25 }, new[] { 0.5 }, new[] { 0.75 }, new[] { 1.0 } };
            var y = new[] { 1e-12, 1.5e-12, 2e-12, 2.5e-12, 3e-12 };
            var r = SolveCore.SolveInverse(
                X, y.Select(v => new[] { v }).ToArray(), new[] { 0 },
                new[] { new[] { double.NaN } },
                new[] { new[] { 4.5e-12 } },
                new[] { new[] { 0.0, 1.0 } },
                "linear", 42L, 10);
            r[0, 3].Should().Be(1.0);
        }

        [Fact]
        public void SolveInverse_Underdetermined_HitsTargetWithoutUniqueSolution()
        {
            // y = 2 + u1 + u2: one target, two adjustable columns → solution set is a line.
            int n = 20;
            var X = Enumerable.Range(1, n)
                .Select(i => new[] { (double)i, (double)(i * 7 % 13) }).ToArray();
            var y = X.Select(r => 2.0 + r[0] + r[1]).ToArray();
            var r2 = SolveCore.SolveInverse(
                X, y.Select(v => new[] { v }).ToArray(), new[] { 0, 1 },
                new[] { new[] { double.NaN, double.NaN } },
                new[] { new[] { 13.0 } },
                new[] { new[] { 0.0, 20.0 }, new[] { 0.0, 20.0 } },
                "linear", 42L, 10);
            r2[0, 2].Should().BeApproximately(13.0, 1e-6); // prediction column (v=2 → index 2+0)
            r2[0, 0].Should().BeInRange(0.0, 20.0);
            r2[0, 1].Should().BeInRange(0.0, 20.0);
        }

        [Fact]
        public void SolveInverse_SameSeed_IsDeterministic()
        {
            var a = SolveLinear(13.0, seed: 7L);
            var b = SolveLinear(13.0, seed: 7L);
            a.Cast<double>().Should().Equal(b.Cast<double>());
        }

        [Fact]
        public void SolveInverse_NoTarget_throws()
        {
            var Y = YLin.Select(v => new[] { v }).ToArray();
            var act = () => SolveCore.SolveInverse(
                XLin, Y, new[] { 1 }, new[] { new[] { 10.0, double.NaN } },
                new[] { new[] { double.NaN } }, new[] { new[] { 2.0, 20.0 } },
                "linear", 42L, 10);
            act.Should().Throw<ArgumentException>().WithMessage("*no output target*");
        }

        [Fact]
        public void SolveInverse_BadBounds_throws()
        {
            var Y = YLin.Select(v => new[] { v }).ToArray();
            var act = () => SolveCore.SolveInverse(
                XLin, Y, new[] { 1 }, new[] { new[] { 10.0, double.NaN } },
                new[] { new[] { 13.0 } }, new[] { new[] { 20.0, 2.0 } },
                "linear", 42L, 10);
            act.Should().Throw<ArgumentException>().WithMessage("*lower > upper*");
        }

        [Fact]
        public void SolveInverse_BadStartCount_throws()
        {
            var Y = YLin.Select(v => new[] { v }).ToArray();
            var act = () => SolveCore.SolveInverse(
                XLin, Y, new[] { 1 }, new[] { new[] { 10.0, double.NaN } },
                new[] { new[] { 13.0 } }, new[] { new[] { 2.0, 20.0 } },
                "linear", 42L, 0);
            act.Should().Throw<ArgumentException>().WithMessage("*max_starts*");
        }

        // ──────────────────────────── Task 1.5 速率模型（ADR-0008）────────────────────────────

        // Out = In − t·(0.01 + 0.005·U1); t alternates 30/60; features = [Incoming, U1, Aux, Time].
        private static double[][] RateX(out double[] y)
        {
            var X = new double[12][];
            y = new double[12];
            for (int i = 0; i < 12; i++)
            {
                double inc = 10 + 0.1 * (i + 1), u1 = 2 + (i % 6), t = i % 2 == 0 ? 30.0 : 60.0;
                X[i] = new[] { inc, u1, 1 + 0.2 * i, t };
                y[i] = inc - t * (0.01 + 0.005 * u1);
            }
            return X;
        }

        private static object[,] RateTable(int requestTime = 90, double requestTarget = 7.3)
        {
            var t = new object[14, 4];
            t[0, 0] = "IncomingZ1"; t[0, 1] = "VariableU1"; t[0, 2] = "FixedTime"; t[0, 3] = "OutputZ1";
            for (int i = 0; i < 12; i++)
            {
                double inc = 10 + 0.1 * (i + 1), u1 = 2 + (i % 6), time = i % 2 == 0 ? 30.0 : 60.0;
                t[i + 1, 0] = inc; t[i + 1, 1] = u1; t[i + 1, 2] = time;
                t[i + 1, 3] = inc - time * (0.01 + 0.005 * u1);
            }
            t[13, 0] = 10.0; t[13, 1] = null!; t[13, 2] = (double)requestTime; t[13, 3] = requestTarget;
            return t;
        }

        [Fact]
        public void FitModel_Rate_ExactAndExtrapolates()
        {
            var X = RateX(out var y);
            var m = SolveCore.FitModel(X, y, "rate", 0, 3);
            m.Kind.Should().Be("rate");
            SolveCore.Predict(m, new[] { 10.0, 4.0, 1.0, 90.0 }).Should().BeApproximately(7.3, 1e-9);
            SolveCore.PredictRate(m, new[] { 10.0, 4.0, 1.0, 90.0 }).Should().BeApproximately(0.03, 1e-12);
        }

        [Fact]
        public void CrossValidate_Rate_Exact()
        {
            var X = RateX(out var y);
            var (scheme, r2, _) = SolveCore.CrossValidate(X, y, "rate", 42L, 0, 3);
            scheme.Should().Be("LOO"); // n=12 < 20
            r2.Should().BeGreaterThanOrEqualTo(0.999);
        }

        [Fact]
        public void FitAuto_PicksRate_WhenPolyExcluded()
        {
            var X = RateX(out var y); // k=4 → poly 14 terms; LOO minTrain 11 < 16 → skipped
            var r = SolveCore.FitAuto(X, y, 42L, 0, 3);
            r.PolySkipped.Should().BeTrue();
            r.RateSkipped.Should().BeFalse();
            r.Chosen.Should().Be("rate");
            r.R2.Should().BeGreaterThanOrEqualTo(0.999);
        }

        [Fact]
        public void FitModel_Rate_MissingPositions_throws()
        {
            var X = RateX(out var y);
            var act = () => SolveCore.FitModel(X, y, "rate");
            act.Should().Throw<ArgumentException>().WithMessage("*pair*");
        }

        [Fact]
        public void Inverse_Rate_ExtrapolatesToArbitraryTime()
        {
            var result = (object[,])SolveUdf.UDF_SOLVE_INVERSE(RateTable(), null, null, "rate", 42.0, 10.0);
            result.GetLength(1).Should().Be(6); // 请求行, U1, 预测, 速率, σ, 状态
            result[0, 2].Should().Be("OutputZ1预测");
            result[0, 3].Should().Be("OutputZ1速率");
            ((double)result[1, 1]).Should().BeApproximately(4.0, 1e-4);
            ((double)result[1, 2]).Should().BeApproximately(7.3, 1e-6);
            ((double)result[1, 3]).Should().BeApproximately(0.03, 1e-9);
            result[1, 5].Should().Be("可达");
        }

        [Fact]
        public void Inverse_Rate_MissingPairing_throws()
        {
            object[,] data = {
                {"IncomingA","VariableU1","FixedTime","OutputY1"},
                {10.0, 2.0, 30.0, 9.0},
                {10.0, 4.0, 60.0, 8.8},
                {10.0, 6.0, 30.0, 8.4},
                {10.0, 8.0, 60.0, 8.2},
                {10.0, 2.0, 60.0, 9.4},
                {10.0, null, 90.0, 7.3},
            };
            var act = () => SolveCore.Inverse(data, null, null, "rate", 42L, 10);
            act.Should().Throw<ArgumentException>().WithMessage("*matching Incoming*");
        }

        [Fact]
        public void Inverse_Rate_MissingTimeColumn_throws()
        {
            object[,] data = {
                {"IncomingZ1","VariableU1","OutputZ1"},
                {10.0, 2.0, 9.4},
                {10.0, 4.0, 8.8},
                {10.0, 6.0, 8.2},
                {10.0, 2.0, 9.6},
                {10.0, null, 7.3},
            };
            var act = () => SolveCore.Inverse(data, null, null, "rate", 42L, 10);
            act.Should().Throw<ArgumentException>().WithMessage("*time column*");
        }

        [Fact]
        public void Inverse_Rate_NonPositiveTime_throws()
        {
            var act = () => SolveCore.Inverse(RateTable(requestTime: 0), null, null, "rate", 42L, 10);
            act.Should().Throw<ArgumentException>().WithMessage("*positive finite time*");
        }

        [Fact]
        public void Inverse_Rate_VariableTime_IsSolved()
        {
            // VariableTime 空白 + bounds [60,120] → 求解器自行选择时间；目标 6.4 在 t·r 可达域内。
            var data = new object[14, 4];
            data[0, 0] = "IncomingZ1"; data[0, 1] = "VariableU1"; data[0, 2] = "VariableTime"; data[0, 3] = "OutputZ1";
            for (int i = 0; i < 12; i++)
            {
                double inc = 10 + 0.1 * (i + 1), u1 = 2 + (i % 6), time = i % 2 == 0 ? 30.0 : 60.0;
                data[i + 1, 0] = inc; data[i + 1, 1] = u1; data[i + 1, 2] = time;
                data[i + 1, 3] = inc - time * (0.01 + 0.005 * u1);
            }
            data[13, 0] = 10.0; data[13, 1] = null!; data[13, 2] = null!; data[13, 3] = 6.4;
            object[,] bounds = { { "VariableTime", 60.0, 120.0 } };
            var r = (object[,])SolveUdf.UDF_SOLVE_INVERSE(data, null, bounds, "rate");
            int statusCol = r.GetLength(1) - 1;
            // 列序：请求行 | VariableU1 | VariableTime | OutputZ1预测 | OutputZ1速率 | σ | 状态
            ((double)r[1, 2]).Should().BeInRange(60.0, 120.0);
            ((double)r[1, 3]).Should().BeApproximately(6.4, 1e-4);
            r[1, statusCol].Should().Be("可达");
        }

        [Fact]
        public void Quality_ExplicitRate_CandidateRow()
        {
            var q = (object[,])SolveUdf.UDF_SOLVE_QUALITY(RateTable(), "rate", 42.0);
            q.GetLength(0).Should().Be(2); // header + rate
            q[1, 1].Should().Be("rate");
            q[1, 5].Should().Be("是");
        }

        [Fact]
        public void Equation_Rate_ForwardAndRateEquations()
        {
            var t = (object[,])SolveUdf.UDF_SOLVE_EQUATION(RateTable(), "rate");
            t.GetLength(0).Should().Be(3); // header + forward + rate
            t[1, 1].Should().Be("前向方程");
            t[1, 2].Should().Be("OutputZ1(FixedTime) = IncomingZ1 - FixedTime*(0.01 + 0.005*VariableU1)");
            t[2, 1].Should().Be("速率方程");
            t[2, 2].Should().Be("OutputZ1速率 = 0.01 + 0.005*VariableU1");
        }

        // ──────────────────────────── UDF 装配层 ────────────────────────────

        [Fact]
        public void Inverse_EmbeddedRequest_TableLayout()
        {
            var table = (object[,])SolveUdf.UDF_SOLVE_INVERSE(LinearTable(true));
            table.GetLength(0).Should().Be(2);
            table.GetLength(1).Should().Be(5);
            table[0, 0].Should().Be("请求行");
            table[0, 1].Should().Be("VariableU1");
            table[0, 2].Should().Be("OutputY1预测");
            table[0, 3].Should().Be("最大偏差σ");
            table[0, 4].Should().Be("状态");
            table[1, 0].Should().Be("数据第12行");
            ((double)table[1, 1]).Should().BeApproximately(4.0, 1e-4);
            ((double)table[1, 2]).Should().BeApproximately(13.0, 1e-6);
            table[1, 4].Should().Be("可达");
        }

        [Fact]
        public void Inverse_SeparateRequestTable_SameResult()
        {
            var history = LinearTable(false);
            object[,] request = {
                {"IncomingA","VariableU1","OutputY1"},
                {10.0, null!, 13.0},
            };
            var a = SolveCore.Inverse(LinearTable(true), null!, null!, "auto", 42L, 10);
            var b = SolveCore.Inverse(history, request, null!, "auto", 42L, 10);
            // 前者 data 内嵌请求行标签为“数据第12行”，后者独立表标签为“请求1”。
            a[1, 0].Should().Be("数据第12行");
            b[1, 0].Should().Be("请求1");
            ((double)b[1, 1]).Should().BeApproximately((double)a[1, 1], 1e-10);
            ((double)b[1, 2]).Should().BeApproximately((double)a[1, 2], 1e-10);
            b[1, 4].Should().Be("可达");
        }

        [Fact]
        public void Inverse_FixedBlank_UsesHistoryMedian()
        {
            // y = 1 + 2a + 3u + 4f; f history 1..10 → median 5.5.
            object[,] Build(object fixedCell)
            {
                var t = new object[12, 4];
                t[0, 0] = "IncomingA"; t[0, 1] = "VariableU1"; t[0, 2] = "FixedF"; t[0, 3] = "OutputY1";
                for (int i = 0; i < 10; i++)
                {
                    double a = i + 1, u = ((a * 3) % 10) + 2.0, f = ((a * 7) % 10) + 1.0;
                    t[i + 1, 0] = a; t[i + 1, 1] = u; t[i + 1, 2] = f;
                    t[i + 1, 3] = 1 + 2 * a + 3 * u + 4 * f;
                }
                t[11, 0] = 10.0; t[11, 1] = null!; t[11, 2] = fixedCell; t[11, 3] = 58.0;
                return t;
            }
            var blank = SolveCore.Inverse(Build(null!), null!, null!, "auto", 42L, 10);
            var explicitMedian = SolveCore.Inverse(Build(5.5), null, null, "auto", 42L, 10);
            for (int c = 1; c <= 3; c++)
                ((double)blank[1, c]).Should().Be((double)explicitMedian[1, c]);
            blank[1, 4].Should().Be("可达");
        }

        [Fact]
        public void Inverse_BoundsTableByNameAndIndex()
        {
            var history = LinearTable(false);
            object[,] request = {
                {"IncomingA","VariableU1","OutputY1"},
                {10.0, null!, 13.0},
            };
            object[,] boundsByName = { { "VariableU1", 2.0, 20.0 } };
            object[,] boundsByIndex = { { 1.0, 2.0, 20.0 } };
            var a = (object[,])SolveUdf.UDF_SOLVE_INVERSE(history, request, boundsByName);
            var b = (object[,])SolveUdf.UDF_SOLVE_INVERSE(history, request, boundsByIndex);
            ((double)a[1, 1]).Should().BeApproximately(4.0, 1e-4);
            ((double)b[1, 1]).Should().BeApproximately((double)a[1, 1], 1e-12);
        }

        [Fact]
        public void Inverse_VariableAfterFixed_MapsDataColumnsToFeaturePositions()
        {
            // 列序 IncomingA | FixedF | VariableU1 | OutputY1 → Features = [0,2,1]：
            // 可调列数据索引 2 对应特征位置 1（固定列插在可调列之前时二者不相等）。
            var t = new object[12, 4];
            t[0, 0] = "IncomingA"; t[0, 1] = "FixedF"; t[0, 2] = "VariableU1"; t[0, 3] = "OutputY1";
            for (int i = 0; i < 10; i++)
            {
                double a = i + 1, f = ((a * 7) % 10) + 1.0, u = UOf(i + 1);
                t[i + 1, 0] = a; t[i + 1, 1] = f; t[i + 1, 2] = u;
                t[i + 1, 3] = 1 + 0.5 * a + 2 * f + 1.5 * u;
            }
            t[11, 0] = 10.0; t[11, 1] = null!; t[11, 2] = null!; t[11, 3] = 23.0;
            var r = (object[,])SolveUdf.UDF_SOLVE_INVERSE(t);
            ((double)r[1, 1]).Should().BeApproximately(4.0, 1e-4);
            ((double)r[1, 2]).Should().BeApproximately(23.0, 1e-6);
            r[1, 4].Should().Be("可达");
        }

        [Fact]
        public void Inverse_NoRequestRows_throws()
        {
            var act = () => SolveCore.Inverse(LinearTable(false), null!, null!, "auto", 42L, 10);
            act.Should().Throw<ArgumentException>().WithMessage("*No request rows*");
        }

        [Fact]
        public void PredictTable_ReturnsMatrixWidthOutputs()
        {
            var history = LinearTable(false);
            object[,] values = {
                {10.0, 4.0},
                {1.0, 2.0},
            };
            var r = (double[,])SolveUdf.UDF_SOLVE_PREDICT(history, values);
            r.GetLength(0).Should().Be(2);
            r.GetLength(1).Should().Be(1);
            r[0, 0].Should().BeApproximately(13.0, 1e-9);
            r[1, 0].Should().BeApproximately(5.5, 1e-9);
        }

        [Fact]
        public void PredictTable_WrongWidth_throws()
        {
            var history = LinearTable(false);
            object[,] values = { { 10.0, 4.0, 1.0 } };
            var act = () => SolveCore.PredictTable(history, values, "auto");
            act.Should().Throw<ArgumentException>().WithMessage("*2 columns*");
        }

        [Fact]
        public void Quality_Auto_PolySkippedRow()
        {
            var X = FitAutoFixture(33, 11, out var y);
            var data = new object[34, 12];
            data[0, 0] = "OutputY1";
            for (int j = 0; j < 11; j++) data[0, j + 1] = "VariableU" + (j + 1);
            for (int i = 0; i < 33; i++)
            {
                data[i + 1, 0] = y[i];
                for (int j = 0; j < 11; j++) data[i + 1, j + 1] = X[i][j];
            }
            var t = (object[,])SolveUdf.UDF_SOLVE_QUALITY(data);
            t.GetLength(0).Should().Be(4); // header + linear + poly + rate
            t.GetLength(1).Should().Be(6);
            t[0, 0].Should().Be("输出");
            t[0, 5].Should().Be("选用");
            t[1, 1].Should().Be("linear");
            t[1, 5].Should().Be("是");
            t[2, 1].Should().Be("poly");
            t[2, 2].Should().Be("跳过");
            t[2, 5].Should().Be("否");
            t[3, 1].Should().Be("rate");
            t[3, 2].Should().Be("跳过");
            t[3, 5].Should().Be("否");
        }

        [Fact]
        public void Equation_LinearSingleVariable_ForwardAndInverse()
        {
            var t = (object[,])SolveUdf.UDF_SOLVE_EQUATION(LinearTable(false));
            t.GetLength(0).Should().Be(3); // header + forward + inverse
            t[0, 0].Should().Be("输出");
            t[0, 1].Should().Be("类型");
            t[0, 2].Should().Be("表达式");
            t[1, 1].Should().Be("前向方程");
            t[1, 2].Should().Be("OutputY1 = 2 + 0.5*IncomingA + 1.5*VariableU1");
            t[2, 1].Should().Be("反解公式");
            t[2, 2].Should().Be("VariableU1 = (OutputY1 - 2 - 0.5*IncomingA) / 1.5");
        }

        [Fact]
        public void Equation_MultiVariable_ForwardOnly()
        {
            var X = FitAutoFixture(33, 11, out var y);
            var data = new object[34, 12];
            data[0, 0] = "OutputY1";
            for (int j = 0; j < 11; j++) data[0, j + 1] = "VariableU" + (j + 1);
            for (int i = 0; i < 33; i++)
            {
                data[i + 1, 0] = y[i];
                for (int j = 0; j < 11; j++) data[i + 1, j + 1] = X[i][j];
            }
            var t = (object[,])SolveUdf.UDF_SOLVE_EQUATION(data);
            t.GetLength(0).Should().Be(2); // header + forward only (11 variables → no closed-form inverse)
            t[1, 1].Should().Be("前向方程");
            ((string)t[1, 2]).Should().Contain("OutputY1 = ");
        }
    }
}

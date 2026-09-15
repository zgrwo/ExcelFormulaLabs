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

        [Fact]
        public void FitAuto_PolyNotSkipped_WhenMinTrainEqualsTermsPlusOne()
        {
            // poly 走 Ridge（增广 QR），最小训练折 n = terms+1（k=2 → 5 项 → 6 行）
            // 恰好满足 FitModel 的 n ≥ terms+1；判据若为 minTrain < terms+2 会把该边界误判为跳过。
            var X = new[]
            {
                new[] { 0.0, 0.0 }, new[] { 1.0, 2.0 }, new[] { 2.0, 1.0 },
                new[] { 3.0, 4.0 }, new[] { 4.0, 3.0 }, new[] { 5.0, 6.0 }, new[] { 6.0, 5.0 }
            };
            var y = X.Select(r => 1.0 + 2.0 * r[0] + 3.0 * r[1]
                + 0.5 * r[0] * r[0] + 0.25 * r[1] * r[1] + 0.1 * r[0] * r[1]).ToArray();
            var (_, chosen, _, r2, _, polySkipped, rateSkipped) = SolveCore.FitAuto(X, y, 42L);
            polySkipped.Should().BeFalse();
            rateSkipped.Should().BeTrue();
            chosen.Should().Be("poly");
            r2.Should().BeGreaterThanOrEqualTo(0.999);
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

        // rate 变体：时间列角色为 VariableTime，请求行 t 留空（由 bounds 约束参与寻优）。
        private static object[,] VariableTimeTable()
        {
            var t = new object[14, 4];
            t[0, 0] = "IncomingZ1"; t[0, 1] = "VariableU1"; t[0, 2] = "VariableTime"; t[0, 3] = "OutputZ1";
            for (int i = 0; i < 12; i++)
            {
                double inc = 10 + 0.1 * (i + 1), u1 = 2 + (i % 6), time = i % 2 == 0 ? 30.0 : 60.0;
                t[i + 1, 0] = inc; t[i + 1, 1] = u1; t[i + 1, 2] = time;
                t[i + 1, 3] = inc - time * (0.01 + 0.005 * u1);
            }
            t[13, 0] = 10.0; t[13, 1] = null!; t[13, 2] = null!; t[13, 3] = 6.4;
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
            var result = (object[,])SolveUdf.UDF_SOLVE_INVERSE(RateTable(), null!, null!, "rate", 42.0, 10.0);
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
                {10.0, null!, 90.0, 7.3},
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
                {10.0, null!, 7.3},
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
            var r = (object[,])SolveUdf.UDF_SOLVE_INVERSE(data, null!, bounds, "rate");
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
            // 夹具 u=2+(i%7)（与 a 无精确线性关系）：u=((3a)%10)+2 与 f=((7a)%10)+1 会使
            // 剔除第 10 行后的 LOO 训练折精确秩亏（rank 3/4），被秩守卫拒绝 →
            // linear 候选跳过，夹具失效。保持夹具意图（f 中位数仍 5.5、u=5 可达）。
            object[,] Build(object fixedCell)
            {
                var t = new object[12, 4];
                t[0, 0] = "IncomingA"; t[0, 1] = "VariableU1"; t[0, 2] = "FixedF"; t[0, 3] = "OutputY1";
                for (int i = 0; i < 10; i++)
                {
                    double a = i + 1, u = 2 + (i % 7), f = ((a * 7) % 10) + 1.0;
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
            // 夹具 u=2+(i%7)（避免 LOO 折精确秩亏被秩守卫拒绝）；
            // f 中位数仍 5.5、u=4 仍可达。
            var t = new object[12, 4];
            t[0, 0] = "IncomingA"; t[0, 1] = "FixedF"; t[0, 2] = "VariableU1"; t[0, 3] = "OutputY1";
            for (int i = 0; i < 10; i++)
            {
                double a = i + 1, f = ((a * 7) % 10) + 1.0, u = 2 + (i % 7);
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

        [Fact]
        public void FitModel_HugeScaleFeature_IsNotSilentlyDropped()
        {
            // x~1e200 时原始偏差平方溢出会把有效列静默当常量剔除（逐行预测全错）。
            var v = new[] { 3.0, 6, 2, 7, 1, 5, 8, 4 };
            var X = Enumerable.Range(0, 8).Select(i => new[] { 1e200 * v[i], (double)(i + 1) }).ToArray();
            var y = Enumerable.Range(0, 8).Select(i => 1.0 + 1e-200 * X[i][0]).ToArray();
            var m = SolveCore.FitModel(X, y, "linear");
            m.Coef[0].Should().BeApproximately(1e-200, 1e-212);
            m.Intercept.Should().BeApproximately(1.0, 1e-9);
            for (int i = 0; i < X.Length; i++)
                SolveCore.Predict(m, X[i]).Should().BeApproximately(y[i], 1e-9);
        }

        [Fact]
        public void FitModel_Poly_LargeButRepresentableFeature_Works()
        {
            // x~1e100 → x²=1e200 可表示，但偏差平方溢出会导致平方项被静默剔除。
            var X = Enumerable.Range(1, 8).Select(i => new[] { i * 1e100 }).ToArray();
            var y = X.Select(r => 1.0 + r[0] / 1e100 + 0.5 * (r[0] / 1e100) * (r[0] / 1e100)).ToArray();
            var m = SolveCore.FitModel(X, y, "poly");
            m.Kind.Should().Be("poly");
            for (int i = 0; i < X.Length; i++)
                SolveCore.Predict(m, X[i]).Should().BeApproximately(y[i], 1e-3);
        }

        [Fact]
        public void FitModel_Poly_NonRepresentableTerm_throws()
        {
            // 项值超出 double（x² 溢出）必须显式报错，禁止静默丢列。
            var X = Enumerable.Range(1, 8).Select(i => new[] { i * 1e160 }).ToArray();
            var y = X.Select(r => r[0] / 1e160).ToArray();
            var act = () => SolveCore.FitModel(X, y, "poly");
            act.Should().Throw<ArgumentException>().WithMessage("*not representable*");
        }

        [Fact]
        public void FitModel_TinyScaleFeature_IsNotSilentlyDropped()
        {
            // x~1e-170 时原始偏差平方下溢为精确 0，会把有效列静默当常量剔除 →
            // 退化为仅截距模型（逐行预测全错）。
            var v = new[] { 3.0, 6, 2, 7, 1, 5, 8, 4 };
            var X = Enumerable.Range(0, 8).Select(i => new[] { 1e-170 * v[i] }).ToArray();
            var y = Enumerable.Range(0, 8).Select(i => 1.0 + 1e170 * X[i][0]).ToArray();
            var m = SolveCore.FitModel(X, y, "linear");
            m.Coef[0].Should().BeApproximately(1e170, 1e158);
            m.Intercept.Should().BeApproximately(1.0, 1e-9);
            for (int i = 0; i < X.Length; i++)
                SolveCore.Predict(m, X[i]).Should().BeApproximately(y[i], 1e-9);
        }

        [Fact]
        public void Inverse_AutoRate_InvalidVariableTimeBound_FallsBackToNonRate()
        {
            // auto 下时间下界 ≤0 → rate 候选应跳过并保持 linear/poly 可用（ADR-0008）。
            object[,] bounds = { { "VariableTime", 0.0, 120.0 } };
            var r = (object[,])SolveUdf.UDF_SOLVE_INVERSE(VariableTimeTable(), null!, bounds, "auto");
            r.GetLength(1).Should().Be(6); // 请求行 | U1 | Time | 预测 | σ | 状态（无速率列）
            for (int c = 0; c < r.GetLength(1); c++)
                (r[0, c]?.ToString() ?? string.Empty).Should().NotContain("速率");
        }

        [Fact]
        public void Inverse_Rate_NonPositiveVariableTimeBound_throws()
        {
            // 显式 rate 下时间下界 ≤0 仍按契约报错（auto 才回退）。
            object[,] bounds = { { "VariableTime", 0.0, 120.0 } };
            var act = () => SolveCore.Inverse(VariableTimeTable(), null, bounds, "rate", 42L, 10);
            act.Should().Throw<ArgumentException>().WithMessage("*positive time bound*");
        }

        [Fact]
        public void Inverse_AutoRate_NonPositiveRequestTime_FallsBackToNonRate()
        {
            // FixedTime 请求行 t=0 → auto 退回 linear/poly（显式 rate 仍报错，见上）。
            var r = (object[,])SolveUdf.UDF_SOLVE_INVERSE(RateTable(requestTime: 0), null!, null!, "auto");
            for (int c = 0; c < r.GetLength(1); c++)
                (r[0, c]?.ToString() ?? string.Empty).Should().NotContain("速率");
        }

        [Fact]
        public void PredictTable_Rate_NonPositiveTime_throws()
        {
            // PREDICT 显式 rate 时须逐行校验 t>0：否则静默按 t≤0 外推。
            object[,] values = { { 10.0, 4.0, 0.0 } };
            var act = () => SolveCore.PredictTable(RateTable(), values, "rate");
            act.Should().Throw<ArgumentException>().WithMessage("*positive finite time*");
        }

        [Fact]
        public void PredictTable_AutoRate_ExtrapolatesManualFixture()
        {
            // F11：user-manual 速率示例的 PREDICT 断言（auto 自动选 rate，t=120 → 6.4）。
            object[,] values = { { 10.0, 4.0, 120.0 } };
            var r = (double[,])SolveUdf.UDF_SOLVE_PREDICT(RateTable(), values);
            r[0, 0].Should().BeApproximately(6.4, 1e-6);
        }

        [Fact]
        public void Inverse_MultiOutput_TargetsSubset_SigmaOnlyTargeted()
        {
            // F11：两输出仅 Y1 有目标 → σ 只统计 Y1，Y2 仍给预测。
            var t = MultiOutputTable(out2Target: null);
            var r = (object[,])SolveUdf.UDF_SOLVE_INVERSE(t);
            r.GetLength(1).Should().Be(6); // 请求行 | U1 | Y1预测 | Y2预测 | σ | 状态
            ((double)r[1, 1]).Should().BeApproximately(4.0, 1e-4);
            ((double)r[1, 2]).Should().BeApproximately(13.0, 1e-6);
            ((double)r[1, 3]).Should().BeApproximately(-6.0, 1e-6); // Y2 = 3 − 10 + 0.25·4
            ((double)r[1, 4]).Should().BeLessThan(1e-6);
            r[1, 5].Should().Be("可达");
        }

        [Fact]
        public void Inverse_MultiOutput_OneUnreachable_StatusUnreachable()
        {
            // F11：Y2 目标 −8 在边界 [2,20] 内不可达 → 状态不可达，推荐为折中解。
            var t = MultiOutputTable(out2Target: -8.0);
            var r = (object[,])SolveUdf.UDF_SOLVE_INVERSE(t);
            r[1, 5].Should().Be("不可达");
            ((double)r[1, 4]).Should().BeGreaterThan(0.01);
        }

        [Fact]
        public void Quality_MultiOutput_RowsPerOutput()
        {
            // F11：auto 下每输出 3 候选行（linear/poly/rate），共 1 + 3×2 行。
            var t = (object[,])SolveUdf.UDF_SOLVE_QUALITY(MultiOutputTable(out2Target: null));
            t.GetLength(0).Should().Be(7);
            t[0, 0].Should().Be("输出");
            t[1, 0].Should().Be("OutputY1");
            t[4, 0].Should().Be("OutputY2");
            t[1, 1].Should().Be("linear");
            t[2, 1].Should().Be("poly");
            t[3, 1].Should().Be("rate");
        }

        [Fact]
        public void PredictTable_TooManyFeatureColumns_throws()
        {
            var data = WideLinearTable();
            object[,] values = new object[1, 51];
            for (int c = 0; c < 51; c++) values[0, c] = 1.0;
            var act = () => SolveCore.PredictTable(data, values, "linear");
            act.Should().Throw<ArgumentException>().WithMessage("*Too many feature columns*");
        }

        [Fact]
        public void Quality_TooManyFeatureColumns_throws()
        {
            var act = () => SolveCore.Quality(WideLinearTable(), "linear", 42L);
            act.Should().Throw<ArgumentException>().WithMessage("*Too many feature columns*");
        }

        [Fact]
        public void Equation_TooManyFeatureColumns_throws()
        {
            var act = () => SolveCore.Equation(WideLinearTable(), "linear");
            act.Should().Throw<ArgumentException>().WithMessage("*Too many feature columns*");
        }

        // F5 守卫夹具：Incoming + 50 个 Variable + Output = 51 个特征列（上限 50）。
        private static object[,] WideLinearTable()
        {
            var t = new object[7, 52];
            t[0, 0] = "IncomingA";
            for (int j = 0; j < 50; j++) t[0, j + 1] = "VariableU" + (j + 1);
            t[0, 51] = "OutputY1";
            for (int i = 0; i < 6; i++)
            {
                t[i + 1, 0] = i + 1;
                double sum = 0;
                for (int j = 0; j < 50; j++) { double v = ((i + 1) * (j + 1)) % 7 + 1; t[i + 1, j + 1] = v; sum += v; }
                t[i + 1, 51] = 1 + sum;
            }
            return t;
        }

        // F11 夹具：Y1 = 2 + 0.5a + 1.5u；Y2 = 3 − a + 0.25u；请求行 a=10、u 留空。
        private static object[,] MultiOutputTable(double? out2Target)
        {
            var t = new object[12, 4];
            t[0, 0] = "IncomingA"; t[0, 1] = "VariableU1"; t[0, 2] = "OutputY1"; t[0, 3] = "OutputY2";
            for (int i = 0; i < 10; i++)
            {
                double a = i + 1, u = ((a * 3) % 10) + 2.0;
                t[i + 1, 0] = a; t[i + 1, 1] = u;
                t[i + 1, 2] = 2.0 + 0.5 * a + 1.5 * u;
                t[i + 1, 3] = 3.0 - a + 0.25 * u;
            }
            t[11, 0] = 10.0; t[11, 1] = null!; t[11, 2] = 13.0;
            t[11, 3] = out2Target.HasValue ? (object)out2Target.Value : null!;
            return t;
        }

        // ──────────────────────── ADR-0009：rate_poly / 多时间列 / SharedOutput ────────────────────────

        // rate_poly 夹具：g = 0.01 + 0.005·U1 + 0.001·U1²，t ∈ {30,60}；请求 t=90、目标 5.86。
        private static object[,] RatePolyTable()
        {
            var t = new object[14, 4];
            t[0, 0] = "IncomingZ1"; t[0, 1] = "VariableU1"; t[0, 2] = "FixedTime"; t[0, 3] = "OutputZ1";
            for (int i = 0; i < 12; i++)
            {
                double inc = 10 + 0.1 * (i + 1), u = 2 + (i % 6), time = i % 2 == 0 ? 30.0 : 60.0;
                double rate = 0.01 + 0.005 * u + 0.001 * u * u;
                t[i + 1, 0] = inc; t[i + 1, 1] = u; t[i + 1, 2] = time; t[i + 1, 3] = inc - time * rate;
            }
            t[13, 0] = 10.0; t[13, 1] = null!; t[13, 2] = 90.0; t[13, 3] = 5.86;
            return t;
        }

        // 多时间列夹具：OutputZ1 ↔ FixedTimeZ1、OutputZ2 ↔ FixedTimeZ2（后缀配对）；
        // g1 = 0.01+0.005u、g2 = 0.02+0.002u；请求 t1=90/t2=45、目标 7.3/18.74（u=4）。
        private static object[,] MultiTimeTable()
        {
            var t = new object[14, 7];
            t[0, 0] = "IncomingZ1"; t[0, 1] = "IncomingZ2"; t[0, 2] = "VariableU1";
            t[0, 3] = "FixedTimeZ1"; t[0, 4] = "FixedTimeZ2"; t[0, 5] = "OutputZ1"; t[0, 6] = "OutputZ2";
            for (int i = 0; i < 12; i++)
            {
                double inc1 = 10 + 0.1 * (i + 1), inc2 = 20 + 0.2 * (i + 1), u = 2 + (i % 6);
                double t1 = i % 2 == 0 ? 30.0 : 60.0, t2 = i % 2 == 0 ? 15.0 : 30.0;
                t[i + 1, 0] = inc1; t[i + 1, 1] = inc2; t[i + 1, 2] = u;
                t[i + 1, 3] = t1; t[i + 1, 4] = t2;
                t[i + 1, 5] = inc1 - t1 * (0.01 + 0.005 * u);
                t[i + 1, 6] = inc2 - t2 * (0.02 + 0.002 * u);
            }
            t[13, 0] = 10.0; t[13, 1] = 20.0; t[13, 2] = null!;
            t[13, 3] = 90.0; t[13, 4] = 45.0; t[13, 5] = 7.3; t[13, 6] = 18.74;
            return t;
        }

        // SharedOutput 夹具：A/B 两输出共享 g = 0.01+0.005u；单一 FixedTime；请求 t=90、目标 7.3/9.3。
        private static object[,] SharedRateTable()
        {
            var t = new object[14, 6];
            t[0, 0] = "IncomingA"; t[0, 1] = "IncomingB"; t[0, 2] = "VariableU1";
            t[0, 3] = "FixedTime"; t[0, 4] = "SharedOutputA"; t[0, 5] = "SharedOutputB";
            for (int i = 0; i < 12; i++)
            {
                double incA = 10 + 0.1 * (i + 1), incB = 12 + 0.1 * (i + 1), u = 2 + (i % 6);
                double time = i % 2 == 0 ? 30.0 : 60.0, g = 0.01 + 0.005 * u;
                t[i + 1, 0] = incA; t[i + 1, 1] = incB; t[i + 1, 2] = u; t[i + 1, 3] = time;
                t[i + 1, 4] = incA - time * g;
                t[i + 1, 5] = incB - time * g;
            }
            t[13, 0] = 10.0; t[13, 1] = 12.0; t[13, 2] = null!; t[13, 3] = 90.0;
            t[13, 4] = 7.3; t[13, 5] = 9.3;
            return t;
        }

        [Fact]
        public void FitModel_RatePoly_ExactQuadraticRate()
        {
            // 3 特征 [inc, u, t] → 排除 inc/t 后 g 的候选项 = 2（线性 + 平方），n=12 ≥ 3。
            var X = new double[12][];
            var y = new double[12];
            for (int i = 0; i < 12; i++)
            {
                double inc = 10 + 0.1 * (i + 1), u = 2 + (i % 6), t = i % 2 == 0 ? 30.0 : 60.0;
                X[i] = new[] { inc, u, t };
                y[i] = inc - t * (0.01 + 0.005 * u + 0.001 * u * u);
            }
            var m = SolveCore.FitModel(X, y, "rate_poly", 0, 2);
            m.Kind.Should().Be("rate_poly");
            m.IsRate.Should().BeTrue();
            SolveCore.Predict(m, new[] { 10.0, 4.0, 90.0 }).Should().BeApproximately(5.86, 1e-3);
            SolveCore.PredictRate(m, new[] { 10.0, 4.0, 90.0 }).Should().BeApproximately(0.046, 1e-5);
        }

        // 排除必须按幂向量判定——若用 `excluded.Contains(t)` 把展开项号当特征列号，
        // inc²/t²/inc·t 等会泄漏（系数非 0），t 外推 9000 时预测 -2037.34（正确 -404）。
        [Fact]
        public void FitModel_RatePoly_ExcludedColumnsNever_leak_in_any_power_or_interaction()
        {
            var X = new double[12][];
            var y = new double[12];
            for (int i = 0; i < 12; i++)
            {
                double inc = 10 + 0.1 * (i + 1), u = 2 + (i % 6), t = i % 2 == 0 ? 30.0 : 60.0;
                X[i] = new[] { inc, u, t };
                y[i] = inc - t * (0.01 + 0.005 * u + 0.001 * u * u);
            }
            var m = SolveCore.FitModel(X, y, "rate_poly", 0, 2);
            // 项号映射（BuildPowers 顺序）：0 inc, 1 u, 2 t, 3 inc², 4 u², 5 t², 6 inc·u, 7 inc·t, 8 u·t
            foreach (int t in new[] { 0, 2, 3, 5, 6, 7, 8 })
                m.Coef[t].Should().Be(0.0, $"term {t} involves excluded feature 0 or 2");
            // 允许项精确恢复 g 的二次项。
            m.Coef[1].Should().BeApproximately(0.005, 1e-5);
            m.Coef[4].Should().BeApproximately(0.001, 1e-5);
            // 外推不再被泄漏项污染：正确 10 − 9000·0.046 = −404。
            SolveCore.Predict(m, new[] { 10.0, 4.0, 9000.0 }).Should().BeApproximately(-404.0, 1e-2);
        }

        [Fact]
        public void Inverse_RatePoly_Extrapolates()
        {
            var r = (object[,])SolveUdf.UDF_SOLVE_INVERSE(RatePolyTable(), null!, null!, "rate_poly");
            r.GetLength(1).Should().Be(6); // 请求行 | U1 | 预测 | 速率 | σ | 状态
            ((double)r[1, 1]).Should().BeApproximately(4.0, 1e-3);
            ((double)r[1, 2]).Should().BeApproximately(5.86, 1e-3);
            ((double)r[1, 3]).Should().BeApproximately(0.046, 1e-4);
            r[1, 5].Should().Be("可达");
        }

        [Fact]
        public void Inverse_MultipleTimeColumns_SuffixPairing()
        {
            var r = (object[,])SolveUdf.UDF_SOLVE_INVERSE(MultiTimeTable(), null!, null!, "rate");
            // 请求行 | U1 | OutputZ1预测 | OutputZ2预测 | Z1速率 | Z2速率 | σ | 状态
            r.GetLength(1).Should().Be(8);
            ((double)r[1, 1]).Should().BeApproximately(4.0, 1e-4);
            ((double)r[1, 2]).Should().BeApproximately(7.3, 1e-6);
            ((double)r[1, 3]).Should().BeApproximately(18.74, 1e-6);
            ((double)r[1, 4]).Should().BeApproximately(0.03, 1e-9);
            ((double)r[1, 5]).Should().BeApproximately(0.028, 1e-9);
            r[1, 7].Should().Be("可达");
        }

        [Fact]
        public void Inverse_MultipleTimeColumns_UnmatchedOutput_throws()
        {
            object[,] data = {
                {"IncomingY1","VariableU1","FixedTimeZ1","FixedTimeZ2","OutputY1"},
                {10.0, 2.0, 30.0, 15.0, 9.4},
                {10.0, 3.0, 60.0, 30.0, 8.8},
                {10.0, 4.0, 30.0, 15.0, 9.2},
                {10.0, 5.0, 60.0, 30.0, 8.6},
                {10.0, null!, 90.0, 45.0, 7.3},
            };
            var act = () => SolveCore.Inverse(data, null, null, "rate", 42L, 10);
            act.Should().Throw<ArgumentException>().WithMessage("*cannot bind a time column*");
        }

        [Fact]
        public void Inverse_SharedOutput_PoolsRate()
        {
            var r = (object[,])SolveUdf.UDF_SOLVE_INVERSE(SharedRateTable(), null!, null!, "rate");
            // 请求行 | U1 | A预测 | B预测 | A速率 | B速率 | σ | 状态
            r.GetLength(1).Should().Be(8);
            ((double)r[1, 1]).Should().BeApproximately(4.0, 1e-4);
            ((double)r[1, 2]).Should().BeApproximately(7.3, 1e-6);
            ((double)r[1, 3]).Should().BeApproximately(9.3, 1e-6);
            ((double)r[1, 4]).Should().BeApproximately(0.03, 1e-9); // 同组共享同一 g
            ((double)r[1, 5]).Should().BeApproximately(0.03, 1e-9);
            r[1, 7].Should().Be("可达");

            // auto：共享组在 rate/rate_poly 池化候选中选优，语义与显式 rate 一致。
            var auto = (object[,])SolveUdf.UDF_SOLVE_INVERSE(SharedRateTable(), null!, null!, "auto");
            auto.GetLength(1).Should().Be(8);
            ((double)auto[1, 1]).Should().BeApproximately(4.0, 1e-3);
            ((double)auto[1, 4]).Should().BeApproximately(0.03, 1e-9);
            auto[1, 7].Should().Be("可达");
        }

        [Fact]
        public void Quality_SharedOutput_PooledCandidates()
        {
            var t = (object[,])SolveUdf.UDF_SOLVE_QUALITY(SharedRateTable());
            t.GetLength(0).Should().Be(5); // header + 2 个共享输出 × (rate, rate_poly)
            t[0, 0].Should().Be("输出");
            t[1, 0].Should().Be("SharedOutputA");
            t[1, 1].Should().Be("rate");
            t[2, 1].Should().Be("rate_poly");
            t[3, 0].Should().Be("SharedOutputB");
            t[1, 5].Should().Be("是");
        }

        [Fact]
        public void Equation_SharedOutput_SharedRateEquation()
        {
            var t = (object[,])SolveUdf.UDF_SOLVE_EQUATION(SharedRateTable(), "rate");
            t.GetLength(0).Should().Be(5); // header + 2 输出 × (前向方程 + 速率方程)
            t[1, 0].Should().Be("SharedOutputA");
            t[2, 1].Should().Be("速率方程");
            ((string)t[2, 2]).Should().Be("SharedOutputA速率 = 0.01 + 0.005*VariableU1");
            t[4, 1].Should().Be("速率方程");
            ((string)t[4, 2]).Should().Be("SharedOutputB速率 = 0.01 + 0.005*VariableU1");
        }

        // 共享池化 CV 数组夹具：单时间列，成员 A/B 共享 g；noisy=true 时注入确定性扰动。
        private static (double[][] X, double[][] Y, int[][] Pairs) SharedCvArrays(bool noisy)
        {
            int n = 12;
            var X = new double[n][]; var Y = new double[n][];
            for (int i = 0; i < n; i++)
            {
                double incA = 10 + 0.1 * (i + 1), incB = 12 + 0.1 * (i + 1), u = 2 + (i % 6);
                double tm = i % 2 == 0 ? 30.0 : 60.0, g = 0.01 + 0.005 * u;
                double noise = noisy ? 0.002 * (((i * 7) % 5) - 2) : 0.0;
                X[i] = new[] { incA, incB, u, tm };
                Y[i] = new[] { incA - tm * g + noise, incB - tm * g - noise };
            }
            return (X, Y, new[] { new[] { 0, 3 }, new[] { 1, 3 } });
        }

        [Fact]
        public void CrossValidateShared_ExactFixture_R2One()
        {
            var (X, Y, pairs) = SharedCvArrays(noisy: false);
            var cv = SolveCore.CrossValidateShared(X, Y, new[] { 0, 1 }, pairs, "rate", 42L);
            cv.Scheme.Should().Be("5折");
            cv.R2.Should().BeApproximately(1.0, 1e-9);
            cv.Mae.Should().BeLessThan(1e-10);
        }

        [Fact]
        public void CrossValidateShared_NoisyFixture_MatchesHardcodedReference()
        {
            // 期望值经独立 Python 复刻（同 XorShift64 折划分 + 朴素 OLS）复核：
            // R²=0.999994043219849、MAE=0.0025083679315728（相对差 ~1.5e-16）。
            var (X, Y, pairs) = SharedCvArrays(noisy: true);
            var cv = SolveCore.CrossValidateShared(X, Y, new[] { 0, 1 }, pairs, "rate", 42L);
            cv.Scheme.Should().Be("5折");
            cv.R2.Should().BeApproximately(0.999994043219849, 1e-9);
            cv.Mae.Should().BeApproximately(0.0025083679315728, 1e-12);
        }

        [Fact]
        public void CrossValidateShared_NegativeHistoryTime_throws()
        {
            // 池化 CV 与 FitSharedRate 必须同语义拒绝 t≤0（防 QUALITY/INVERSE 行为分裂）。
            var (X, _, pairs) = SharedCvArrays(noisy: false);
            for (int i = 0; i < X.Length; i++) X[i][3] = -X[i][3];
            var Y = new double[X.Length][];
            for (int i = 0; i < X.Length; i++)
            {
                double incA = X[i][0], incB = X[i][1], u = X[i][2], tm = X[i][3];
                Y[i] = new[] { incA - tm * (0.01 + 0.005 * u), incB - tm * (0.01 + 0.005 * u) };
            }
            var act = () => SolveCore.CrossValidateShared(X, Y, new[] { 0, 1 }, pairs, "rate", 42L);
            act.Should().Throw<ArgumentException>().WithMessage("*positive finite time*");
            var act2 = () => SolveCore.FitSharedRate(X, Y, new[] { 0, 1 }, pairs, "rate", null);
            act2.Should().Throw<ArgumentException>().WithMessage("*positive finite time*");
        }

        [Fact]
        public void CrossValidateShared_RaggedOrNonFiniteMatrix_throws()
        {
            // 共享入口与 CrossValidate 同款矩阵防御。
            var ragged = new[] { new[] { 1.0, 2.0, 3.0, 4.0 }, new[] { 1.0, 2.0 } };
            var Y = new[] { new[] { 1.0, 2.0 }, new[] { 1.0, 2.0 } };
            int[][] pairs = { new[] { 0, 1 } };
            var act = () => SolveCore.CrossValidateShared(ragged, Y, new[] { 0 }, pairs, "rate", 42L);
            act.Should().Throw<ArgumentException>().WithMessage("*length differs*");
            var act2 = () => SolveCore.FitSharedRate(ragged, Y, new[] { 0 }, pairs, "rate", null);
            act2.Should().Throw<ArgumentException>().WithMessage("*length differs*");

            var nonFinite = new[] { new[] { 1.0, 2.0, 3.0, 4.0 }, new[] { 1.0, double.NaN, 3.0, 4.0 }, new[] { 1.0, 2.0, 3.0, 4.0 } };
            var Y3 = new[] { new[] { 1.0, 2.0 }, new[] { 1.0, 2.0 }, new[] { 1.0, 2.0 } };
            var act3 = () => SolveCore.CrossValidateShared(nonFinite, Y3, new[] { 0 }, pairs, "rate", 42L);
            act3.Should().Throw<ArgumentException>().WithMessage("*non-finite*");
        }

        [Fact]
        public void FitAuto_RankDeficientLinear_SkipsToRate()
        {
            // inc2 = 2×inc1（精确共线）→ linear 秩亏跳过；poly 展开超样本跳过；rate 可用。
            var X = new double[12][];
            var y = new double[12];
            for (int i = 0; i < 12; i++)
            {
                double inc1 = 10 + 0.1 * (i + 1), inc2 = 2 * inc1, u = 2 + (i % 6);
                double t1 = i % 2 == 0 ? 30.0 : 60.0, t2 = i % 2 == 0 ? 15.0 : 30.0;
                X[i] = new[] { inc1, inc2, u, t1, t2 };
                y[i] = inc1 - t1 * (0.01 + 0.005 * u);
            }
            var r = SolveCore.FitAuto(X, y, 42L, 0, 3);
            r.Chosen.Should().Be("rate");
            r.RateSkipped.Should().BeFalse();
            r.PolySkipped.Should().BeTrue();
        }

        [Fact]
        public void Quality_RankDeficientLinear_SkipsToRate()
        {
            var t = (object[,])SolveUdf.UDF_SOLVE_QUALITY(MultiTimeTable());
            t.GetLength(0).Should().Be(7);
            t[1, 0].Should().Be("OutputZ1");
            t[1, 1].Should().Be("linear");
            t[1, 2].Should().Be("跳过"); // 秩亏 → 跳过而非整体 #VALUE!
            t[3, 1].Should().Be("rate");
            t[3, 5].Should().Be("是");
            t[4, 0].Should().Be("OutputZ2");
            t[6, 1].Should().Be("rate");
            t[6, 5].Should().Be("是");
        }

        [Fact]
        public void Inverse_RankDeficientLinear_AutoSelectsRate()
        {
            var r = (object[,])SolveUdf.UDF_SOLVE_INVERSE(MultiTimeTable(), null!, null!, "auto");
            r.GetLength(1).Should().Be(8);
            ((double)r[1, 1]).Should().BeApproximately(4.0, 1e-4);
            ((double)r[1, 2]).Should().BeApproximately(7.3, 1e-6);
            ((double)r[1, 3]).Should().BeApproximately(18.74, 1e-6);
            r[1, 7].Should().Be("可达");
        }

        [Fact]
        public void FitModel_ConstantColumnRounding_NotCollinear()
        {
            // R2-2：常量列 0.1（n=3）的 Σc/n 舍入（0.10000000000000002 ≠ 0.1）产生伪 ss>0，
            // 标准化后与截距精确共线 → 整表 near-singular #VALUE!。修复：min==max 位级常量判定。
            var X = new double[][]
            {
                new[] { 1.0, 0.1 }, new[] { 2.0, 0.1 }, new[] { 3.0, 0.1 },
            };
            var y = new double[] { 2.0, 4.0, 6.0 };
            var m = SolveCore.FitModel(X, y, "linear");
            SolveCore.Predict(m, new[] { 4.0, 0.1 }).Should().BeApproximately(8.0, 1e-10);
            // 0.3（n=5）此前正常，一并守回归。
            var X5 = new double[5][];
            var y5 = new double[5];
            for (int i = 0; i < 5; i++) { X5[i] = new[] { i + 1.0, 0.3 }; y5[i] = 2 * (i + 1.0); }
            var m5 = SolveCore.FitModel(X5, y5, "linear");
            SolveCore.Predict(m5, new[] { 6.0, 0.3 }).Should().BeApproximately(12.0, 1e-10);
        }

        [Fact]
        public void CrossValidateShared_HugeScale_ThrowsNotSaturated()
        {
            // R2-3：共享池化 CV 缺非有限守卫时，1e160 尺度 Y 的 sse/tss 上溢 → 1−Inf/NaN
            // 静默饱和（R²=1，与非池化 CrossValidate 的显式拒绝矛盾）。
            var (X, Y, pairs) = SharedCvArrays(noisy: true);
            for (int i = 0; i < X.Length; i++)
            {
                for (int j = 0; j < X[i].Length; j++) X[i][j] *= 1e160;
                for (int j = 0; j < Y[i].Length; j++) Y[i][j] *= 1e160;
            }
            var act = () => SolveCore.CrossValidateShared(X, Y, new[] { 0, 1 }, pairs, "rate", 42L);
            act.Should().Throw<ArgumentException>();
        }
    }
}

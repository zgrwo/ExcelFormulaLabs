using System;
using System.Linq;
using ExcelFormulaLabs.Analytics;
using ExcelFormulaLabs.Foundation;
using FluentAssertions;
using Xunit;

namespace ExcelFormulaLabs.Analytics.Tests
{
    public class SolveUdfTests
    {
        private static object[,] History()
        {
            var t = new object[11, 3];
            t[0, 0] = "IncomingA"; t[0, 1] = "VariableU1"; t[0, 2] = "OutputY1";
            for (int i = 0; i < 10; i++)
            {
                double a = i + 1, u = (a * 3) % 10 + 2.0;
                t[i + 1, 0] = a;
                t[i + 1, 1] = u;
                t[i + 1, 2] = 2.0 + 0.5 * a + 1.5 * u;
            }
            return t;
        }

        private static object[,] Request()
        {
            return new object[,] {
                {"IncomingA","VariableU1","OutputY1"},
                {10.0, null!, 13.0},
            };
        }

        [Fact]
        public void Inverse_ValidInput_ReturnsTable()
        {
            var r = SolveUdf.UDF_SOLVE_INVERSE(History(), Request());
            r.Should().BeOfType<object[,]>();
            var t = (object[,])r;
            t.GetLength(0).Should().Be(2);
            t.GetLength(1).Should().Be(5);
        }

        [Fact]
        public void Inverse_NullData_ReturnsValueError()
        {
            SolveUdf.UDF_SOLVE_INVERSE(null!).Should().Be(ExcelError.Value);
        }

        [Fact]
        public void Inverse_NoVariableColumn_ReturnsValueError()
        {
            object[,] data = { { "IncomingA", "OutputY1" }, { 1.0, 2.0 } };
            SolveUdf.UDF_SOLVE_INVERSE(data).Should().Be(ExcelError.Value);
        }

        [Fact]
        public void Inverse_NoRequestRows_ReturnsValueError()
        {
            SolveUdf.UDF_SOLVE_INVERSE(History()).Should().Be(ExcelError.Value);
        }

        [Fact]
        public void Inverse_UnknownModel_ReturnsValueError()
        {
            SolveUdf.UDF_SOLVE_INVERSE(History(), Request(), null!, "gpr", null!, null!).Should().Be(ExcelError.Value);
        }

        [Fact]
        public void Inverse_MaxStartsOutOfRange_ReturnsValueError()
        {
            SolveUdf.UDF_SOLVE_INVERSE(History(), Request(), null!, null!, null!, 0).Should().Be(ExcelError.Value);
        }

        [Fact]
        public void Predict_ValidInput_ReturnsMatrix()
        {
            object[,] values = { { 10.0, 4.0 } };
            var r = SolveUdf.UDF_SOLVE_PREDICT(History(), values);
            r.Should().BeOfType<double[,]>();
            var m = (double[,])r;
            m.GetLength(0).Should().Be(1);
            m.GetLength(1).Should().Be(1);
            m[0, 0].Should().BeApproximately(13.0, 1e-9);
        }

        [Fact]
        public void Predict_EmptyValues_ReturnsValueError()
        {
            SolveUdf.UDF_SOLVE_PREDICT(History(), null!).Should().Be(ExcelError.Value);
        }

        [Fact]
        public void Quality_ValidInput_HeaderContract()
        {
            var r = SolveUdf.UDF_SOLVE_QUALITY(History());
            r.Should().BeOfType<object[,]>();
            var t = (object[,])r;
            t[0, 0].Should().Be("输出");
            t[0, 1].Should().Be("候选");
            t[0, 2].Should().Be("CV方案");
            t[0, 3].Should().Be("CV_R2");
            t[0, 4].Should().Be("CV_MAE");
            t[0, 5].Should().Be("选用");
        }

        [Fact]
        public void Quality_NoHistory_ReturnsValueError()
        {
            object[,] data = {
                {"IncomingA","VariableU1","OutputY1"},
                {10.0, null!, 13.0},
            };
            SolveUdf.UDF_SOLVE_QUALITY(data).Should().Be(ExcelError.Value);
        }

        [Fact]
        public void Equation_ValidInput_HeaderContract()
        {
            var r = SolveUdf.UDF_SOLVE_EQUATION(History());
            r.Should().BeOfType<object[,]>();
            var t = (object[,])r;
            t[0, 0].Should().Be("输出");
            t[0, 1].Should().Be("类型");
            t[0, 2].Should().Be("表达式");
        }

        [Fact]
        public void Equation_UnknownModel_ReturnsValueError()
        {
            SolveUdf.UDF_SOLVE_EQUATION(History(), "poly2").Should().Be(ExcelError.Value);
        }
    }
}

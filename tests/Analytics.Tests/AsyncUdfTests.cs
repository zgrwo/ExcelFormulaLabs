using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using ExcelDna.Integration;
using ExcelFormulaLabs.Analytics;
using ExcelFormulaLabs.Foundation;
using FluentAssertions;
using Xunit;

namespace ExcelFormulaLabs.Analytics.Tests
{
    /// <summary>
    /// The 12 *_ASYNC UDF wrappers
    /// call ExcelAsyncUtil.Run, which requires a live Excel host (throws
    /// InvalidOperationException otherwise) - so the wrapper itself is not unit-testable.
    /// What IS testable:
    ///   1. AnalyticsHelpers.DictToReport - the report-table conversion used by the
    ///      async REGRESS UDFs (and nowhere else in tests).
    ///   2. The [ExcelFunction] registration contract of all 12 async UDFs.
    ///   3. The documented non-Excel behaviour (guard against accidental behaviour change).
    /// </summary>
    public class AsyncUdfTests
    {
        // -- 1. DictToReport (async REGRESS path) --

        [Fact]
        public void DictToReport_builds_row_major_report()
        {
            var dict = new Dictionary<string, object>
            {
                ["coefficients"] = new double[] { 1.0, 2.0 },
                ["sse"] = 0.5,
                ["n"] = 3L
            };
            var report = AnalyticsHelpers.DictToReport(dict);
            report.GetLength(0).Should().Be(3);
            report.GetLength(1).Should().Be(3);
            report[0, 0].Should().Be("coefficients");
            report[0, 1].Should().Be(1.0);
            report[0, 2].Should().Be(2.0);
            report[1, 0].Should().Be("sse");
            report[1, 1].Should().Be(0.5);
            report[2, 0].Should().Be("n");
            report[2, 1].Should().Be(3L);
        }

        [Fact]
        public void DictToReport_scalar_fields_span_single_column()
        {
            var dict = new Dictionary<string, object> { ["r_squared"] = 0.9 };
            var report = AnalyticsHelpers.DictToReport(dict);
            report.GetLength(1).Should().Be(2);
            report[0, 1].Should().Be(0.9);
        }

        [Fact]
        public void DictToReport_empty_dict_returns_zero_rows()
        {
            var report = AnalyticsHelpers.DictToReport(new Dictionary<string, object>());
            report.GetLength(0).Should().Be(0);
        }

        [Fact]
        public void DictToReport_handles_long_array_fields()
        {
            var dict = new Dictionary<string, object> { ["group_counts"] = new long[] { 5, 7 } };
            var report = AnalyticsHelpers.DictToReport(dict);
            report[0, 1].Should().Be(5L);
            report[0, 2].Should().Be(7L);
        }

        // -- 2. Registration contract of all 12 async UDFs --

        private static readonly (string Name, int ParamCount, string[] ParamNames)[] AsyncContract =
        {
            ("LINALG.SVD_U_ASYNC", 1, new[] { "array" }),
            ("LINALG.SVD_S_ASYNC", 1, new[] { "array" }),
            ("LINALG.SVD_VT_ASYNC", 1, new[] { "array" }),
            ("LINALG.QR_Q_ASYNC", 1, new[] { "array" }),
            ("LINALG.QR_R_ASYNC", 1, new[] { "array" }),
            ("LINALG.EIGEN_ASYNC", 1, new[] { "array" }),
            ("LINALG.SOLVE_ASYNC", 2, new[] { "array1", "array2" }),
            ("LINALG.CHOLESKY_ASYNC", 1, new[] { "array" }),
            ("LINALG.PINV_ASYNC", 1, new[] { "array" }),
            ("REGRESS.OLS_ASYNC", 2, new[] { "known_y", "known_x" }),
            ("REGRESS.WLS_ASYNC", 3, new[] { "known_y", "known_x", "weights" }),
            ("REGRESS.RIDGE_ASYNC", 3, new[] { "known_y", "known_x", "[lambda]" }),
        };

        [Fact]
        public void All_async_udfs_registered_with_expected_contract()
        {
            var assembly = typeof(LinalgAsyncUdf).Assembly;
            foreach (var (name, paramCount, paramNames) in AsyncContract)
            {
                var method = FindAsyncMethod(assembly, name);
                method.Should().NotBeNull("async UDF " + name + " must exist");

                var attr = method!.GetCustomAttribute<ExcelFunctionAttribute>();
                attr.Should().NotBeNull();
                attr!.Name.Should().Be(name);

                var args = method.GetParameters();
                args.Length.Should().Be(paramCount, name + " parameter count");
                for (int i = 0; i < paramCount; i++)
                {
                    var argAttr = args[i].GetCustomAttribute<ExcelArgumentAttribute>();
                    argAttr.Should().NotBeNull();
                    argAttr!.Name.Should().Be(paramNames[i]);
                }
            }
        }

        [Fact]
        public void Async_names_match_api_reference()
        {
            // Cross-check against docs/specification/api-reference.md (single source of truth):
            // every *_ASYNC name in the doc must have a matching registration and vice versa.
            var api = File.ReadAllText(Path.Combine(TestRoot(), "docs", "specification", "api-reference.md"));
            var pattern = new Regex(@"\|\s*`((?:LINALG|REGRESS)\.[A-Z_]+_ASYNC)`");
            var docNames = pattern.Matches(api).Cast<Match>().Select(m => m.Groups[1].Value).Distinct().OrderBy(x => x).ToArray();  // Cast: MatchCollection is non-generic on net48
            var codeNames = AsyncContract.Select(c => c.Name).OrderBy(x => x).ToArray();
            docNames.Should().BeEquivalentTo(codeNames, "api-reference async list must match registrations");
        }

        private static MethodInfo? FindAsyncMethod(Assembly asm, string excelName)
        {
            foreach (var type in new[] { typeof(LinalgAsyncUdf), typeof(RegressionAsyncUdf) })
                foreach (var m in type.GetMethods(BindingFlags.Public | BindingFlags.Static))
                {
                    var a = m.GetCustomAttribute<ExcelFunctionAttribute>();
                    if (a != null && a.Name == excelName) return m;
                }
            return null;
        }

        private static string TestRoot()
        {
            var dir = AppContext.BaseDirectory;
            for (int i = 0; i < 8 && dir != null; i++)
            {
                if (File.Exists(Path.Combine(dir, "ExcelFormulaLabs.sln"))) return dir;
                dir = Path.GetDirectoryName(dir);
            }
            return Directory.GetCurrentDirectory();
        }

        // -- 3. Documented non-Excel behaviour --

        [Fact]
        public void Async_udf_failure_outside_excel_host_returns_value_error()
        {
            // 参数转换与 Run 均在 WrapError 之内——无 Excel 宿主时
            // ExcelAsyncUtil.Run 抛 InvalidOperationException，与同步 UDF 一致转为 #VALUE!
            //（否则异常穿出，同步/异步失败语义分叉）。
            LinalgAsyncUdf.UDF_LINALG_SVD_U_ASYNC(new double[,] { { 1 } })
                .Should().Be(ExcelFormulaLabs.Foundation.ExcelError.Value);
        }

        // -- 4. 直接调用全部 12 个 async 包装体 --
        // 上面第 2 组是**反射式契约测试**（只查属性/参数名，不执行方法体）；本组补上
        // "真的调用一遍"：每个包装体的参数准备（M/V/prep）与 WrapError 边界都被执行，
        // 从此 12 个 *_ASYNC 入口不再只有 SVD_U_ASYNC 有直接调用测试。

        /// <summary>
        /// 每个 async 包装体的直接调用夹具。输入用**可通过转换**的形状，
        /// 使路径走到 ExcelAsyncUtil.Run（无宿主 → InvalidOperationException → #VALUE!）。
        /// </summary>
        public static IEnumerable<object?[]> AsyncUdfDirectCalls()
        {
            double[,] spd = { { 4, 1 }, { 1, 3 } };              // 对称正定：EIGEN/CHOLESKY 需要
            double[,] rect = { { 1, 2 }, { 3, 4 }, { 5, 7 } };
            double[,] square = { { 2, 1 }, { 1, 3 } };
            double[] rhs = { 1, 2 };
            double[] y = { 1, 2, 3 };
            double[,] x = { { 1 }, { 2 }, { 3 } };
            double[] w = { 1, 1, 1 };

            yield return new object?[] { "LINALG.SVD_U_ASYNC", (Func<object>)(() => LinalgAsyncUdf.UDF_LINALG_SVD_U_ASYNC(rect)) };
            yield return new object?[] { "LINALG.SVD_S_ASYNC", (Func<object>)(() => LinalgAsyncUdf.UDF_LINALG_SVD_S_ASYNC(rect)) };
            yield return new object?[] { "LINALG.SVD_VT_ASYNC", (Func<object>)(() => LinalgAsyncUdf.UDF_LINALG_SVD_VT_ASYNC(rect)) };
            yield return new object?[] { "LINALG.QR_Q_ASYNC", (Func<object>)(() => LinalgAsyncUdf.UDF_LINALG_QR_Q_ASYNC(rect)) };
            yield return new object?[] { "LINALG.QR_R_ASYNC", (Func<object>)(() => LinalgAsyncUdf.UDF_LINALG_QR_R_ASYNC(rect)) };
            yield return new object?[] { "LINALG.EIGEN_ASYNC", (Func<object>)(() => LinalgAsyncUdf.UDF_LINALG_EIGEN_ASYNC(spd)) };
            yield return new object?[] { "LINALG.SOLVE_ASYNC", (Func<object>)(() => LinalgAsyncUdf.UDF_LINALG_SOLVE_ASYNC(square, rhs)) };
            yield return new object?[] { "LINALG.CHOLESKY_ASYNC", (Func<object>)(() => LinalgAsyncUdf.UDF_LINALG_CHOLESKY_ASYNC(spd)) };
            yield return new object?[] { "LINALG.PINV_ASYNC", (Func<object>)(() => LinalgAsyncUdf.UDF_LINALG_PINV_ASYNC(rect)) };
            yield return new object?[] { "REGRESS.OLS_ASYNC", (Func<object>)(() => RegressionAsyncUdf.UDF_REGRESS_OLS_ASYNC(y, x)) };
            yield return new object?[] { "REGRESS.WLS_ASYNC", (Func<object>)(() => RegressionAsyncUdf.UDF_REGRESS_WLS_ASYNC(y, x, w)) };
            yield return new object?[] { "REGRESS.RIDGE_ASYNC", (Func<object>)(() => RegressionAsyncUdf.UDF_REGRESS_RIDGE_ASYNC(y, x)) };
        }

        [Theory]
        [MemberData(nameof(AsyncUdfDirectCalls))]
        public void Async_wrapper_degrades_to_value_error_without_excel_host(string name, Func<object> call)
        {
            object? result = null;
            Action act = () => result = call();
            act.Should().NotThrow(name + " 不得让异常穿出（同步/异步失败语义不得分叉）");
            result.Should().Be(ExcelFormulaLabs.Foundation.ExcelError.Value,
                name + " 无 Excel 宿主时必须降级为 #VALUE!");
        }

        [Fact]
        public void Async_wrapper_invalid_argument_returns_value_error()
        {
            // 参数准备在 Run 之前（COM/类型转换必须在调用线程完成）——非数值矩阵
            // 由 PrepM 抛错 → WrapError → #VALUE!，与"无宿主"路径同为 #VALUE!。
            LinalgAsyncUdf.UDF_LINALG_SVD_S_ASYNC(new object[,] { { "not-a-number" } })
                .Should().Be(ExcelFormulaLabs.Foundation.ExcelError.Value);
            RegressionAsyncUdf.UDF_REGRESS_OLS_ASYNC(new object[] { "x" }, new object[,] { { 1 } })
                .Should().Be(ExcelFormulaLabs.Foundation.ExcelError.Value);
        }
    }
}
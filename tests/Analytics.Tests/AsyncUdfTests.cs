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
    /// P1-8 (pre-release review): the 12 *_ASYNC UDFs had zero coverage. Their wrappers
    /// call ExcelAsyncUtil.Run, which requires a live Excel host (throws
    /// InvalidOperationException otherwise) - so the wrapper itself is not unit-testable.
    /// What IS testable and previously untested:
    ///   1. AnalyticsHelpers.DictToReport - the report-table conversion used by the
    ///      async REGRESS UDFs (and nowhere else in tests).
    ///   2. The [ExcelFunction] registration contract of all 12 async UDFs.
    ///   3. The documented non-Excel behaviour (guard against accidental behaviour change).
    /// </summary>
    public class AsyncUdfTests
    {
        // -- 1. DictToReport (async REGRESS path, previously 0% covered) --

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
            var api = File.ReadAllText(Path.Combine(TestRoot(), "rules", "api-reference.md"));
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
        public void Async_udf_throws_outside_excel_host()
        {
            // ExcelAsyncUtil.Run requires a live Excel host; outside Excel it throws
            // InvalidOperationException ("not been initialized"). This test documents
            // that behaviour so an implementation change is a conscious decision.
            var act = () => LinalgAsyncUdf.UDF_LINALG_SVD_U_ASYNC(new double[,] { { 1 } });
            act.Should().Throw<InvalidOperationException>();
        }
    }
}
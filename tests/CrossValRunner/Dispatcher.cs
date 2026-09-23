using System.Text.Json;
using ExcelFormulaLabs.Analytics;
using ExcelFormulaLabs.DataToolkit;
using ExcelFormulaLabs.Foundation;

namespace ExcelFormulaLabs.CrossValRunner;

public static class Dispatcher
{
    public delegate object? Invoker(object?[] args, Dictionary<string, object?>? kwargs);

    private static readonly Dictionary<string, Invoker> _map = new();

    static Dispatcher()
    {
        // ═══════════════════ StatsCore ═══════════════════
        Register("StatsCore", "Mean", (a, _) => StatsCore.Mean(ToDouble1D(a[0])));
        Register("StatsCore", "GeometricMean", (a, _) => StatsCore.GeometricMean(ToDouble1D(a[0])));
        Register("StatsCore", "HarmonicMean", (a, _) => StatsCore.HarmonicMean(ToDouble1D(a[0])));
        Register("StatsCore", "Median", (a, _) => StatsCore.Median(ToDouble1D(a[0])));
        Register("StatsCore", "VarianceP", (a, _) => StatsCore.VarianceP(ToDouble1D(a[0])));
        Register("StatsCore", "Variance", (a, _) => StatsCore.Variance(ToDouble1D(a[0])));
        Register("StatsCore", "StdevP", (a, _) => StatsCore.StdevP(ToDouble1D(a[0])));
        Register("StatsCore", "Stdev", (a, _) => StatsCore.Stdev(ToDouble1D(a[0])));
        Register("StatsCore", "Skewness", (a, _) => StatsCore.Skewness(ToDouble1D(a[0])));
        Register("StatsCore", "Kurtosis", (a, _) => StatsCore.Kurtosis(ToDouble1D(a[0])));
        Register("StatsCore", "Min", (a, _) => StatsCore.Min(ToDouble1D(a[0])));
        Register("StatsCore", "Max", (a, _) => StatsCore.Max(ToDouble1D(a[0])));
        Register("StatsCore", "Range", (a, _) => StatsCore.Range(ToDouble1D(a[0])));
        Register("StatsCore", "Sum", (a, _) => StatsCore.Sum(ToDouble1D(a[0])));
        Register("StatsCore", "Product", (a, _) => StatsCore.Product(ToDouble1D(a[0])));
        Register("StatsCore", "Mode", (a, _) => StatsCore.Mode(ToDouble1D(a[0])));
        Register("StatsCore", "CovarianceP", (a, _) => StatsCore.CovarianceP(ToDouble1D(a[0]), ToDouble1D(a[1])));
        Register("StatsCore", "Covariance", (a, _) => StatsCore.Covariance(ToDouble1D(a[0]), ToDouble1D(a[1])));
        Register("StatsCore", "Summary", (a, _) => StatsCore.Summary(ToDouble1D(a[0])));
        Register("StatsCore", "Percentile", (a, _) => StatsCore.Percentile(ToDouble1D(a[0]), ToDouble(a[1])));
        Register("StatsCore", "IQR", (a, _) => StatsCore.IQR(ToDouble1D(a[0])));
        Register("StatsCore", "Pearson", (a, _) => StatsCore.Pearson(ToDouble1D(a[0]), ToDouble1D(a[1])));
        Register("StatsCore", "Spearman", (a, _) => StatsCore.Spearman(ToDouble1D(a[0]), ToDouble1D(a[1])));
        Register("StatsCore", "CorrelationMatrix", (a, _) => StatsCore.CorrelationMatrix(ToDouble2D(a[0])));
        Register("StatsCore", "TTestOneSample", (a, _) => StatsCore.TTestOneSample(ToDouble1D(a[0]), ToDouble(a[1])));
        Register("StatsCore", "TTestTwoSample", (a, _) => StatsCore.TTestTwoSample(ToDouble1D(a[0]), ToDouble1D(a[1])));
        Register("StatsCore", "ZScore", (a, _) => StatsCore.ZScore(ToDouble1D(a[0])));
        // 初等函数接通活体对照（manifest 条目 STATS.ABS/SQRT/LN/LOG10/EXP/SIGN）。
        Register("StatsCore", "Abs", (a, _) => ToDouble1D(a[0]).Select(Math.Abs).ToArray());
        Register("StatsCore", "SqrtSafe", (a, _) => ToDouble1D(a[0]).Select(StatsCore.SqrtSafe).ToArray());
        Register("StatsCore", "LogSafe", (a, _) => ToDouble1D(a[0]).Select(StatsCore.LogSafe).ToArray());
        Register("StatsCore", "Log10Safe", (a, _) => ToDouble1D(a[0]).Select(StatsCore.Log10Safe).ToArray());
        Register("StatsCore", "ExpSafe", (a, _) => ToDouble1D(a[0]).Select(StatsCore.ExpSafe).ToArray());
        Register("StatsCore", "Sign", (a, _) => ToDouble1D(a[0]).Select(StatsCore.Sign).ToArray());

        // ═══════════════════ RegressionCore ═══════════════════
        Register("RegressionCore", "FitOLS", (a, k) =>
            RegressionCore.FitOLS(ToDouble2D(a[0]), ToDouble1D(a[1]),
                Kwarg(k, "addIntercept", true)));
        Register("RegressionCore", "FitWLS", (a, k) =>
            RegressionCore.FitWLS(ToDouble2D(a[0]), ToDouble1D(a[1]), ToDouble1D(a[2]),
                Kwarg(k, "addIntercept", true)));
        Register("RegressionCore", "FitRidge", (a, k) =>
            RegressionCore.FitRidge(ToDouble2D(a[0]), ToDouble1D(a[1]),
                Kwarg(k, "lambda", 1.0), Kwarg(k, "addIntercept", true)));
        Register("RegressionCore", "AnovaOneWay", (a, _) =>
            RegressionCore.AnovaOneWay(ToDoubleJagged(a[0])));
        Register("RegressionCore", "FactorImportance", (a, _) =>
            RegressionCore.FactorImportance(ToDouble2D(a[0]), ToDouble1D(a[1])));

        // ═══════════════════ SolveCore（ADR-0007：有界多起点反解）═══════════════════
        Register("SolveCore", "FitModel", (a, _) =>
        {
            var model = SolveCore.FitModel(ToDoubleJagged(a[0]), ToDouble1D(a[1]), ToString(a[2]));
            return new Dictionary<string, object>
            {
                ["kind"] = model.Kind,
                ["coef"] = model.Coef,
                ["intercept"] = model.Intercept,
                ["term_count"] = (long)model.Coef.Length,
            };
        });
        Register("SolveCore", "CrossValidate", (a, k) =>
            SolveCore.CrossValidate(ToDoubleJagged(a[0]), ToDouble1D(a[1]), ToString(a[2]),
                Kwarg(k, "seed", 42L)));
        Register("SolveCore", "SolveInverse", (a, k) =>
            SolveCore.SolveInverse(ToDoubleJagged(a[0]), ToDoubleJagged(a[1]), ToIntArray(a[2]),
                ToDoubleJagged(a[3]), ToDoubleJagged(a[4]), ToDoubleJagged(a[5]),
                ToString(a[6]), Kwarg(k, "seed", 42L), (int)ToLong(a[7])));
        Register("SolveCore", "PredictFit", (a, _) =>
        {
            var model = SolveCore.FitModel(ToDoubleJagged(a[0]), ToDouble1D(a[1]), ToString(a[2]));
            return SolveCore.Predict(model, ToDouble1D(a[3]));
        });
        // rate 模型（ADR-0008）：显式传入配对来料/时间特征位置
        Register("SolveCore", "FitRate", (a, _) =>
        {
            var model = SolveCore.FitModel(ToDoubleJagged(a[0]), ToDouble1D(a[1]), "rate",
                (int)ToLong(a[2]), (int)ToLong(a[3]));
            return new Dictionary<string, object>
            {
                ["kind"] = model.Kind,
                ["coef"] = model.Coef,
                ["intercept"] = model.Intercept,
                ["term_count"] = (long)model.Coef.Length,
                ["rate_incoming"] = (long)model.RateIncomingIndex,
                ["rate_time"] = (long)model.RateTimeIndex,
            };
        });
        Register("SolveCore", "CrossValidateRate", (a, k) =>
            SolveCore.CrossValidate(ToDoubleJagged(a[0]), ToDouble1D(a[1]), "rate",
                Kwarg(k, "seed", 42L), (int)ToLong(a[2]), (int)ToLong(a[3])));
        Register("SolveCore", "PredictRate", (a, _) =>
        {
            var model = SolveCore.FitModel(ToDoubleJagged(a[0]), ToDouble1D(a[1]), "rate",
                (int)ToLong(a[2]), (int)ToLong(a[3]));
            return SolveCore.Predict(model, ToDouble1D(a[4]));
        });
        Register("SolveCore", "SolveInverseRate", (a, k) =>
            SolveCore.SolveInverseFull(ToDoubleJagged(a[0]), ToDoubleJagged(a[1]), ToIntArray(a[2]),
                ToDoubleJagged(a[3]), ToDoubleJagged(a[4]), ToDoubleJagged(a[5]),
                "rate", Kwarg(k, "seed", 42L), (int)ToLong(a[6]),
                new[] { new[] { (int)ToLong(a[7]), (int)ToLong(a[8]) } }, out _));
        Register("SolveCore", "FitRatePolyPredict", (a, _) =>
        {
            var model = SolveCore.FitModel(ToDoubleJagged(a[0]), ToDouble1D(a[1]), "rate_poly",
                (int)ToLong(a[2]), (int)ToLong(a[3]));
            return SolveCore.Predict(model, ToDouble1D(a[4]));
        });
        // ADR-0009：SharedOutput 池化 g（成员共享同一速率函数）
        Register("SolveCore", "FitSharedRate", (a, _) =>
        {
            var members = ToIntArray(a[2]);
            int[][] pairs = ToDoubleJagged(a[3])
                .Select(r => r.Select(v => (int)v).ToArray()).ToArray();
            var model = SolveCore.FitSharedRate(ToDoubleJagged(a[0]), ToDoubleJagged(a[1]),
                members, pairs, ToString(a[4]), null);
            return new Dictionary<string, object>
            {
                ["kind"] = model.Kind,
                ["coef"] = model.Coef,
                ["intercept"] = model.Intercept,
                ["term_count"] = (long)model.Coef.Length,
            };
        });
        // 共享池化 CV 数值锁（QUALITY 共享组候选）
        Register("SolveCore", "CrossValidateShared", (a, k) =>
            SolveCore.CrossValidateShared(ToDoubleJagged(a[0]), ToDoubleJagged(a[1]), ToIntArray(a[2]),
                ToDoubleJagged(a[3]).Select(r => r.Select(v => (int)v).ToArray()).ToArray(),
                ToString(a[4]), Kwarg(k, "seed", 42L)));

        // ═══════════════════ PhyChemCore ═══════════════════
        Register("PhyChemCore", "MolecularWeight", (a, _) =>
            PhyChemCore.MolecularWeight(ToString(a[0])));
        Register("PhyChemCore", "ConvertTemperature", (a, _) =>
            PhyChemCore.ConvertTemperature(ToDouble(a[0]), ToString(a[1]), ToString(a[2])));
        Register("PhyChemCore", "ConvertPressure", (a, _) =>
            PhyChemCore.ConvertPressure(ToDouble(a[0]), ToString(a[1]), ToString(a[2])));
        Register("PhyChemCore", "ConvertVolume", (a, _) =>
            PhyChemCore.ConvertVolume(ToDouble(a[0]), ToString(a[1]), ToString(a[2])));
        Register("PhyChemCore", "ConvertMass", (a, _) =>
            PhyChemCore.ConvertMass(ToDouble(a[0]), ToString(a[1]), ToString(a[2])));
        Register("PhyChemCore", "IdealGasLaw", (a, k) =>
            PhyChemCore.IdealGasLaw(NullableDouble(a[0]), NullableDouble(a[1]),
                NullableDouble(a[2]), NullableDouble(a[3]),
                Kwarg(k, "r", 8.31446261815324 / 101.325)));
        Register("PhyChemCore", "GasToSTP", (a, k) =>
            PhyChemCore.GasToSTP(ToDouble(a[0]), ToDouble(a[1]), ToDouble(a[2]),
                Kwarg(k, "tUnit", "C"), Kwarg(k, "pUnit", "atm")));
        // Density 对照覆盖正常 + 除零哨兵标签路径（PHYCHEM.DENSITY_*）。
        Register("PhyChemCore", "Density", (a, _) => PhyChemCore.Density(ToDouble(a[0]), ToDouble(a[1])));

        // ═══════════════════ LinalgCore ═══════════════════
        Register("LinalgCore", "Determinant", (a, _) => LinalgCore.Determinant(ToDouble2D(a[0])));
        Register("LinalgCore", "Solve", (a, _) => LinalgCore.Solve(ToDouble2D(a[0]), ToDouble1D(a[1])));
        Register("LinalgCore", "MatMul", (a, _) => LinalgCore.MatMul(ToDouble2D(a[0]), ToDouble2D(a[1])));
        Register("LinalgCore", "Transpose", (a, _) => LinalgCore.Transpose(ToDouble2D(a[0])));
        Register("LinalgCore", "Trace", (a, _) => LinalgCore.Trace(ToDouble2D(a[0])));
        // tol 默认对齐 LinalgCore.Rank(:266)/LinalgUdf(:77)/api-reference（0 = 相对容差，
        // MATLAB/numpy 约定）：harness 独用 1e-10 绝对默认会漏掉 Excel 用户实际走的相对默认路径。
        Register("LinalgCore", "Rank", (a, k) => LinalgCore.Rank(ToDouble2D(a[0]), Kwarg(k, "tol", 0d)));
        Register("LinalgCore", "ConditionNumber", (a, _) => LinalgCore.ConditionNumber(ToDouble2D(a[0])));
        Register("LinalgCore", "Eigenvalues", (a, _) => LinalgCore.Eigenvalues(ToDouble2D(a[0])));
        Register("LinalgCore", "Cholesky", (a, _) => LinalgCore.Cholesky(ToDouble2D(a[0])));
        Register("LinalgCore", "Identity", (a, _) => LinalgCore.Identity((int)ToLong(a[0])));
        Register("LinalgCore", "Svd", (a, _) => { var (U,S,Vt)=LinalgCore.Svd(ToDouble2D(a[0])); return new Dictionary<string,object>{{"U",U},{"S",S},{"Vt",Vt}}; });
        Register("LinalgCore", "Qr", (a, _) => { var (Q,R)=LinalgCore.Qr(ToDouble2D(a[0])); return new Dictionary<string,object>{{"Q",Q},{"R",R}}; });
        Register("LinalgCore", "Lu", (a, _) => { var (L,U,P)=LinalgCore.Lu(ToDouble2D(a[0])); return new Dictionary<string,object>{{"L",L},{"U",U},{"P",P}}; });
        Register("LinalgCore", "LuU", (a, _) => LinalgCore.LuU(ToDouble2D(a[0])));
        Register("LinalgCore", "LuP", (a, _) => LinalgCore.LuP(ToDouble2D(a[0])));
        Register("LinalgCore", "PseudoInverse", (a, _) => LinalgCore.PseudoInverse(ToDouble2D(a[0])));

        // ═══════════════════ DoeCore ═══════════════════
        Register("DoeCore", "FullFactorialCoded", (a, _) =>
            DoeCore.FullFactorialCoded(ToIntArray(a[0])));
        Register("DoeCore", "FractionalCoded", (a, _) =>
            DoeCore.FractionalCoded((int)ToLong(a[0])));
        Register("DoeCore", "RsmCcd", (a, _) =>
            DoeCore.RsmCcd((int)ToLong(a[0])));
        Register("DoeCore", "RsmBb", (a, _) =>
            DoeCore.RsmBb((int)ToLong(a[0])));

        Register("DoeAnalysisCore", "Analyze", (a, k) =>
            DoeAnalysisCore.Analyze(ToDouble2D(a[0]), ToDouble1D(a[1]), (int)ToLong(a[2]), Kwarg(k, "quadratic", false)));
        Register("DoeAnalysisCore", "Anova", (a, k) =>
            DoeAnalysisCore.Anova(ToDouble2D(a[0]), ToDouble1D(a[1]), (int)ToLong(a[2]), Kwarg(k, "quadratic", false)));
        Register("DoeAnalysisCore", "Pareto", (a, k) =>
            DoeAnalysisCore.Pareto(ToDouble2D(a[0]), ToDouble1D(a[1]), (int)ToLong(a[2]), Kwarg(k, "quadratic", false)));

        Register("ArrayCore", "Fill", (a, _) => ArrayCore.Fill(a[0]!, ToLong(a[1])));
        Register("ArrayCore", "Sequence", (a, _) => ArrayCore.Sequence(ToDouble(a[0]), ToDouble(a[1]), ToDouble(a[2])));
        Register("StatsCore", "CountNumeric", (a, _) => StatsCore.CountNumeric(ToObjectArray(a[0])));

        Register("PivotCore", "Pivot", (a, _) =>
            PivotCore.Pivot(ToObject2D(a[0]), (int)ToLong(a[1]), (int)ToLong(a[2]), (int)ToLong(a[3]), ToString(a[4])));
        Register("PivotCore", "GroupBy", (a, _) =>
            PivotCore.GroupBy(ToObject2D(a[0]), ToIntArray(a[1]), (int)ToLong(a[2]), ToString(a[3])));
        Register("RangeExportCore", "RangeToJson", (a, _) =>
            RangeExportCore.RangeToJson(ToObject2D(a[0]), ToBool(a[1]), ToBool(a[2])));
        Register("RangeExportCore", "RangeToCsv", (a, _) =>
            RangeExportCore.RangeToCsv(ToObject2D(a[0]), ToString(a[1]), ToBool(a[2])));
        Register("DictOperations", "FromKeys", (a, _) =>
            DictOperations.FromKeys(ToObjectArray(a[0]), ToString(a[1]), StringComparison.OrdinalIgnoreCase)
                .OrderBy(kv => kv.Key, StringComparer.Ordinal)
                .ToDictionary(kv => kv.Key, kv => kv.Value));
        Register("DoeCore", "TaguchiCoded", (a, _) =>
            DoeCore.TaguchiCoded((int)ToLong(a[0]), (int)ToLong(a[1]), (int)ToLong(a[2]), (int)ToLong(a[3])));

        // ═══════════════════ StringCore ═══════════════════
        Register("StringCore", "ReverseString", (a, _) => StringCore.ReverseString(ToString(a[0])));
        Register("StringCore", "LevenshteinDistance", (a, _) => StringCore.LevenshteinDistance(ToString(a[0]), ToString(a[1])));
        Register("StringCore", "Base64Encode", (a, _) => StringCore.Base64Encode(ToString(a[0])));
        Register("StringCore", "Base64Decode", (a, _) => StringCore.Base64Decode(ToString(a[0])));
        Register("StringCore", "Soundex", (a, _) => StringCore.Soundex(ToString(a[0])));
        Register("StringCore", "CountSubstring", (a, k) => StringCore.CountSubstring(ToString(a[0]), ToString(a[1]), Kwarg(k, "cs", true)));
        Register("StringCore", "CommonPrefix", (a, k) => StringCore.CommonPrefix(ToString(a[0]), ToString(a[1]), Kwarg(k, "cs", true)));
        // 纯确定性字符串函数：接通 Python 独立实现对照
        Register("StringCore", "TextJoin", (a, k) => StringCore.TextJoin(ToString(a[0]), Kwarg(k, "skip", false), ToStringArray(a[1])));
        Register("StringCore", "Coalesce", (a, _) => StringCore.Coalesce(ToString(a[0]), ToString(a[1])));
        Register("StringCore", "IsNullOrEmptyStr", (a, _) => StringCore.IsNullOrEmptyStr(ToString(a[0])));
        Register("StringCore", "IsNullOrWhitespaceStr", (a, _) => StringCore.IsNullOrWhitespaceStr(ToString(a[0])));
        Register("StringCore", "UrlEncode", (a, _) => StringCore.UrlEncode(ToString(a[0])));
        Register("StringCore", "UrlDecode", (a, _) => StringCore.UrlDecode(ToString(a[0])));
        Register("StringCore", "HtmlEncode", (a, _) => StringCore.HtmlEncode(ToString(a[0])));
        Register("StringCore", "HtmlDecode", (a, _) => StringCore.HtmlDecode(ToString(a[0])));
        Register("StringCore", "PadLeft", (a, _) => StringCore.PadLeft(ToString(a[0]), (int)ToLong(a[1])));
        Register("StringCore", "PadRight", (a, _) => StringCore.PadRight(ToString(a[0]), (int)ToLong(a[1])));
        // 2026-09-23 覆盖率扩展：以下方法此前无 manifest 对照（UDF 缺口 12 项）
        Register("StringCore", "NormalizeWhitespace", (a, _) => StringCore.NormalizeWhitespace(ToString(a[0])));
        Register("StringCore", "ToTitleCase", (a, _) => StringCore.ToTitleCase(ToString(a[0])));
        Register("StringCore", "RemoveChars", (a, _) => StringCore.RemoveChars(ToString(a[0]), ToString(a[1])));
        Register("StringCore", "KeepChars", (a, _) => StringCore.KeepChars(ToString(a[0]), ToString(a[1])));
        Register("StringCore", "Truncate", (a, k) => StringCore.Truncate(ToString(a[0]), (int)ToLong(a[1]), Kwarg(k, "suffix", "...")));
        Register("StringCore", "StartsWithStr", (a, k) => StringCore.StartsWithStr(ToString(a[0]), ToString(a[1]), Kwarg(k, "cs", true)));
        Register("StringCore", "EndsWithStr", (a, k) => StringCore.EndsWithStr(ToString(a[0]), ToString(a[1]), Kwarg(k, "cs", true)));
        Register("StringCore", "LeftOf", (a, k) => StringCore.LeftOf(ToString(a[0]), ToString(a[1]), Kwarg(k, "n", 1L)));
        Register("StringCore", "RightOf", (a, k) => StringCore.RightOf(ToString(a[0]), ToString(a[1]), Kwarg(k, "n", 1L)));
        Register("StringCore", "ExtractBetween", (a, k) => StringCore.ExtractBetween(ToString(a[0]), ToString(a[1]), ToString(a[2]), Kwarg(k, "n", 1L), Kwarg(k, "inc", false)));
        Register("StringCore", "NthWord", (a, k) => StringCore.NthWord(ToString(a[0]), Kwarg(k, "n", 1L)));
        Register("StringCore", "StripHtml", (a, _) => StringCore.StripHtml(ToString(a[0])));

        // ═══════════════════ DateTimeCore ═══════════════════
        Register("DateTimeCore", "IsoWeekNum", (a, _) => DateTimeCore.IsoWeekNum(ToDateTime(a[0])));
        Register("DateTimeCore", "Easter", (a, _) => DateTimeCore.Easter(ToLong(a[0])));
        Register("DateTimeCore", "IsLeapYear", (a, _) => DateTimeCore.IsLeapYear(ToLong(a[0])));
        Register("DateTimeCore", "AddWorkdays", (a, _) => DateTimeCore.AddWorkdays(ToDateTime(a[0]), ToLong(a[1])));
        Register("DateTimeCore", "NextWorkday", (a, _) => DateTimeCore.NextWorkday(ToDateTime(a[0])));
        // 确定性日期函数：接入 CrossVal Python 对照
        Register("DateTimeCore", "Weekday", (a, _) => DateTimeCore.Weekday(ToDateTime(a[0])));
        Register("DateTimeCore", "WeekdayISO", (a, _) => DateTimeCore.WeekdayISO(ToDateTime(a[0])));
        Register("DateTimeCore", "IsWeekend", (a, _) => DateTimeCore.IsWeekend(ToDateTime(a[0])));
        Register("DateTimeCore", "Quarter", (a, _) => DateTimeCore.Quarter(ToDateTime(a[0])));
        Register("DateTimeCore", "Semester", (a, _) => DateTimeCore.Semester(ToDateTime(a[0])));
        Register("DateTimeCore", "DayOfYear", (a, _) => DateTimeCore.DayOfYear(ToDateTime(a[0])));
        Register("DateTimeCore", "DaysInMonth", (a, _) => DateTimeCore.DaysInMonth(ToLong(a[0]), ToLong(a[1])));
        Register("DateTimeCore", "EndOfMonth", (a, _) => DateTimeCore.EndOfMonth(ToDateTime(a[0])));
        Register("DateTimeCore", "UnixTimestamp", (a, _) => DateTimeCore.UnixTimestamp(ToDateTime(a[0])));
        Register("DateTimeCore", "AgeDays", (a, _) => DateTimeCore.AgeDays(ToDateTime(a[0]), ToDateTime(a[1])));
        Register("DateTimeCore", "DateDiff", (a, _) => DateTimeCore.DateDiff(ToString(a[0]), ToDateTime(a[1]), ToDateTime(a[2])));

        // ═══════════════════ ArrayCore 补注册 ═══════════════════
        Register("ArrayCore", "SortAsc", (a, _) => ArrayCore.Sort(ToObjectArray(a[0]), true, Foundation.ComparerMode.Auto));
        Register("ArrayCore", "SortNum", (a, _) => ArrayCore.Sort(ToObjectArray(a[0]), true, Foundation.ComparerMode.Numeric));
        Register("ArrayCore", "Unique", (a, _) => ArrayCore.Unique(ToObjectArray(a[0])));
        Register("ArrayCore", "IndexOf", (a, _) => ArrayCore.IndexOf(ToObjectArray(a[0]), ToClr(a[1])));
        Register("ArrayCore", "Contains", (a, _) => ArrayCore.Contains(ToObjectArray(a[0]), ToClr(a[1])));
        Register("ArrayCore", "Reverse", (a, _) => ArrayCore.Reverse(ToObjectArray(a[0])));
        Register("ArrayCore", "Count", (a, _) => ArrayCore.Count(ToObjectArray(a[0])));
        Register("ArrayCore", "Concat", (a, _) => ArrayCore.Concat(ToObjectArray(a[0]), ToObjectArray(a[1])));
        Register("ArrayCore", "Flatten2D", (a, _) => ArrayCore.Flatten2D(ToObject2D(a[0]), "R"));
        // 2026-09-23 覆盖率扩展：排序/切片/过滤子 UDF（ARR.SORTASC/SORTDESC/SORTTEXT/SLICE/FILTER*）
        Register("ArrayCore", "SortDesc", (a, _) => ArrayCore.Sort(ToObjectArray(a[0]), false, Foundation.ComparerMode.Auto));
        Register("ArrayCore", "SortText", (a, _) => ArrayCore.Sort(ToObjectArray(a[0]), true, Foundation.ComparerMode.Text));
        Register("ArrayCore", "Slice", (a, _) => ArrayCore.Slice(ToObjectArray(a[0]), ToLong(a[1]), ToLong(a[2])));
        Register("ArrayCore", "Filter", (a, _) => ArrayCore.Filter(ToObjectArray(a[0]), ToClr(a[1]), ToString(a[2])));

        // ═══════════════════ RegexCore ═══════════════════
        Register("RegexCore", "RegexTest", (a, k) => RegexCore.RegexTest(ToString(a[0]), ToString(a[1]),
            Kwarg(k, "ignoreCase", true)));
        Register("RegexCore", "RegexCount", (a, k) => RegexCore.RegexCount(ToString(a[0]), ToString(a[1]),
            Kwarg(k, "ignoreCase", true)));
        Register("RegexCore", "RegexMatch", (a, k) => RegexCore.RegexMatch(ToString(a[0]), ToString(a[1]),
            Kwarg(k, "n", 1L), Kwarg(k, "ic", true)));
        Register("RegexCore", "RegexReplace", (a, k) => RegexCore.RegexReplace(ToString(a[0]), ToString(a[1]), ToString(a[2]),
            Kwarg(k, "n", 0L), Kwarg(k, "ic", true)));
        Register("RegexCore", "RegexSplit", (a, k) => RegexCore.RegexSplit(ToString(a[0]), ToString(a[1]),
            Kwarg(k, "n", 0L), Kwarg(k, "ic", true)));
    }

    public static (object? result, string? error) Invoke(string coreClass, string coreMethod,
        object?[] args, Dictionary<string, object?>? kwargs)
    {
        var key = $"{coreClass}.{coreMethod}";
        if (!_map.TryGetValue(key, out var invoker))
            return (null, $"'{coreMethod}' not registered for '{coreClass}'.");
        try { return (invoker(args, kwargs), null); }
        catch (Exception ex) when (ex is not OutOfMemoryException
            and not StackOverflowException and not AccessViolationException)
        { return (null, $"{ex.GetType().Name}: {ex.Message}"); }
    }

    private static void Register(string cls, string method, Invoker f) => _map[$"{cls}.{method}"] = f;
    private static double ToDouble(object? v) => v is double d ? d : v is JsonElement je ? je.GetDouble() : Convert.ToDouble(v);
    private static long ToLong(object? v) => v is long l ? l : v is JsonElement je ? je.GetInt64() : Convert.ToInt64(v);
    private static bool ToBool(object? v) => v is bool b ? b : v is JsonElement je ? je.GetBoolean() : Convert.ToBoolean(v);
    private static string ToString(object? v) => v is string s ? s : v?.ToString() ?? "";
    private static DateTime ToDateTime(object? v) => v is DateTime dt ? dt : DateTime.Parse(v?.ToString() ?? "");

    private static double[] ToDouble1D(object? v)
    {
        if (v is double[] da) return da;
        if (v is JsonElement je)
            return je.EnumerateArray().Select(e => e.GetDouble()).ToArray();
        throw new ArgumentException($"Cannot convert to double[].");
    }

    /// <summary>Convert a manifest array (JsonElement) to object[] — for Core methods taking object (CountNumeric).</summary>
    private static string[] ToStringArray(object? v)
    {
        var oa = ToObjectArray(v);
        var r = new string[oa.Length];
        for (int i = 0; i < oa.Length; i++) r[i] = oa[i]?.ToString() ?? "";
        return r;
    }

    private static object[,] ToObject2D(object? v)
    {
        if (v is object[,] m) return m;
        if (v is JsonElement je && je.ValueKind == JsonValueKind.Array)
        {
            var rows = je.EnumerateArray().Select(e => e.EnumerateArray()
                .Select<JsonElement, object?>(x => x.ValueKind == JsonValueKind.String ? x.GetString() : x.GetDouble()).ToArray()).ToArray();
            int h = rows.Length, w = rows.Length > 0 ? rows[0].Length : 0;
            var m2 = new object[h, w];
            // JSON 数组元素已解析为 string/double（ValueKind Null 走不到此处——GetDouble 会
            // 响亮抛出），元素不可能为 null，null-forgiving 消除 CS8601。
            for (int i = 0; i < h; i++) for (int j = 0; j < w; j++) m2[i, j] = rows[i][j]!;
            return m2;
        }
        throw new ArgumentException($"Cannot convert to object[,]: {v?.GetType().Name}");
    }

    /// <summary>Convert a raw JSON/CLR scalar to a CLR primitive — JsonElement numbers arrive
    /// as JsonElement from the manifest (ResolveArg only resolves refs). IndexOf/Contains 的
    /// 数值容差探测要求 value 是真实 CLR 数值类型（double/int/long）。</summary>
    private static object ToClr(object? v)
    {
        if (v is JsonElement je)
            return je.ValueKind switch
            {
                JsonValueKind.Number => je.TryGetInt64(out long l) ? l : je.GetDouble(),
                // ValueKind==String 时 GetString() 非 null（JSON 字符串节点），null-forgiving
                // 消除 CS8603；Null 节点落入 `_` 分支返回 GetRawText()。
                JsonValueKind.String => je.GetString()!,
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                _ => je.GetRawText(),
            };
        return v!;
    }

    private static object[] ToObjectArray(object? v)
    {
        if (v is object[] oa) return oa;
        if (v is JsonElement je && je.ValueKind == JsonValueKind.Array)
            return je.EnumerateArray().Select<JsonElement, object?>(e => e.ValueKind switch
            {
                JsonValueKind.Number => e.GetDouble(),
                JsonValueKind.String => e.GetString(),
                JsonValueKind.True or JsonValueKind.False => e.GetBoolean(),
                JsonValueKind.Null => null,
                _ => e.ToString()
            }).ToArray()!;
        throw new ArgumentException($"Cannot convert to object[].");
    }

    private static int[] ToIntArray(object? v)
    {
        if (v is int[] ia) return ia;
        if (v is JsonElement je)
            return je.EnumerateArray().Select(e => e.GetInt32()).ToArray();
        throw new ArgumentException($"Cannot convert to int[].");
    }

    private static double[,] ToDouble2D(object? v)
    {
        if (v is double[,] d2) return d2;
        if (v is JsonElement je)
        {
            var rows = new List<double[]>();
            foreach (var row in je.EnumerateArray())
                rows.Add(row.EnumerateArray().Select(e => e.GetDouble()).ToArray());
            int r = rows.Count, c = rows[0].Length;
            var result = new double[r, c];
            for (int i = 0; i < r; i++) for (int j = 0; j < c; j++) result[i, j] = rows[i][j];
            return result;
        }
        throw new ArgumentException($"Cannot convert to double[,].");
    }

    private static double[][] ToDoubleJagged(object? v)
    {
        if (v is double[][] dj) return dj;
        if (v is JsonElement je)
            return je.EnumerateArray().Select(row =>
                row.EnumerateArray().Select(e => e.GetDouble()).ToArray()).ToArray();
        throw new ArgumentException($"Cannot convert to double[][].");
    }

    private static double? NullableDouble(object? v) =>
        v == null || (v is JsonElement je && je.ValueKind == JsonValueKind.Null) ? null : ToDouble(v);

    private static T Kwarg<T>(Dictionary<string, object?>? kwargs, string key, T defaultValue)
    {
        if (kwargs == null || !kwargs.TryGetValue(key, out var val) || val == null) return defaultValue;
        if (val is T t) return t;
        if (val is JsonElement je)
        {
            if (typeof(T) == typeof(double)) return (T)(object)je.GetDouble();
            if (typeof(T) == typeof(long)) return (T)(object)je.GetInt64();
            if (typeof(T) == typeof(bool)) return (T)(object)je.GetBoolean();
            if (typeof(T) == typeof(string)) return (T)(object)(je.GetString() ?? "");
        }
        try { return (T)Convert.ChangeType(val, typeof(T)); }
        catch (Exception ex) when (ex is not OutOfMemoryException
            and not StackOverflowException and not AccessViolationException)
        { return defaultValue; }
    }
}

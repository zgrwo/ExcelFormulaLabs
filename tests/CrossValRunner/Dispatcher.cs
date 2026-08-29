using System.Text.Json;
using ExcelFormulaLabs.Analytics;
using ExcelFormulaLabs.DataToolkit;

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
                Kwarg(k, "r", 0.082057)));
        Register("PhyChemCore", "GasToSTP", (a, k) =>
            PhyChemCore.GasToSTP(ToDouble(a[0]), ToDouble(a[1]), ToDouble(a[2]),
                Kwarg(k, "tUnit", "C"), Kwarg(k, "pUnit", "atm")));

        // ═══════════════════ LinalgCore ═══════════════════
        Register("LinalgCore", "Determinant", (a, _) => LinalgCore.Determinant(ToDouble2D(a[0])));
        Register("LinalgCore", "Solve", (a, _) => LinalgCore.Solve(ToDouble2D(a[0]), ToDouble1D(a[1])));
        Register("LinalgCore", "MatMul", (a, _) => LinalgCore.MatMul(ToDouble2D(a[0]), ToDouble2D(a[1])));
        Register("LinalgCore", "Transpose", (a, _) => LinalgCore.Transpose(ToDouble2D(a[0])));
        Register("LinalgCore", "Trace", (a, _) => LinalgCore.Trace(ToDouble2D(a[0])));
        Register("LinalgCore", "Rank", (a, k) => LinalgCore.Rank(ToDouble2D(a[0]), Kwarg(k, "tol", 1e-10)));
        Register("LinalgCore", "ConditionNumber", (a, _) => LinalgCore.ConditionNumber(ToDouble2D(a[0])));
        Register("LinalgCore", "Eigenvalues", (a, _) => LinalgCore.Eigenvalues(ToDouble2D(a[0])));
        Register("LinalgCore", "Cholesky", (a, _) => LinalgCore.Cholesky(ToDouble2D(a[0])));
        Register("LinalgCore", "Identity", (a, _) => LinalgCore.Identity((int)ToLong(a[0])));
        Register("LinalgCore", "Svd", (a, _) => { var (U,S,Vt)=LinalgCore.Svd(ToDouble2D(a[0])); return new Dictionary<string,object>{{"U",U},{"S",S},{"Vt",Vt}}; });
        Register("LinalgCore", "Qr", (a, _) => { var (Q,R)=LinalgCore.Qr(ToDouble2D(a[0])); return new Dictionary<string,object>{{"Q",Q},{"R",R}}; });
        Register("LinalgCore", "Lu", (a, _) => { var (L,U,P)=LinalgCore.Lu(ToDouble2D(a[0])); return new Dictionary<string,object>{{"L",L},{"U",U},{"P",P}}; });
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

        // ═══════════════════ DoeAnalysisCore（review-2026-08-29 P2-4：此前分析函数无 cross_check）═══════════════
        Register("DoeAnalysisCore", "Analyze", (a, k) =>
            DoeAnalysisCore.Analyze(ToDouble2D(a[0]), ToDouble1D(a[1]), (int)ToLong(a[2]), Kwarg(k, "quadratic", false)));
        Register("DoeAnalysisCore", "Anova", (a, k) =>
            DoeAnalysisCore.Anova(ToDouble2D(a[0]), ToDouble1D(a[1]), (int)ToLong(a[2]), Kwarg(k, "quadratic", false)));
        Register("DoeAnalysisCore", "Pareto", (a, k) =>
            DoeAnalysisCore.Pareto(ToDouble2D(a[0]), ToDouble1D(a[1]), (int)ToLong(a[2]), Kwarg(k, "quadratic", false)));

        // ═══════════════════ ArrayCore / StatsCore.CountNumeric（review 2026-08-29：ARR.* 与 COUNT 此前无活体对照）═══════════════
        Register("ArrayCore", "Fill", (a, _) => ArrayCore.Fill(a[0]!, ToLong(a[1])));
        Register("ArrayCore", "Sequence", (a, _) => ArrayCore.Sequence(ToDouble(a[0]), ToDouble(a[1]), ToDouble(a[2])));
        Register("StatsCore", "CountNumeric", (a, _) => StatsCore.CountNumeric(ToObjectArray(a[0])));

        // ═══════════════════ StringCore ═══════════════════
        Register("StringCore", "ReverseString", (a, _) => StringCore.ReverseString(ToString(a[0])));
        Register("StringCore", "LevenshteinDistance", (a, _) => StringCore.LevenshteinDistance(ToString(a[0]), ToString(a[1])));
        Register("StringCore", "Base64Encode", (a, _) => StringCore.Base64Encode(ToString(a[0])));
        Register("StringCore", "Base64Decode", (a, _) => StringCore.Base64Decode(ToString(a[0])));
        Register("StringCore", "Soundex", (a, _) => StringCore.Soundex(ToString(a[0])));
        Register("StringCore", "CountSubstring", (a, k) => StringCore.CountSubstring(ToString(a[0]), ToString(a[1]), Kwarg(k, "cs", true)));
        Register("StringCore", "CommonPrefix", (a, k) => StringCore.CommonPrefix(ToString(a[0]), ToString(a[1]), Kwarg(k, "cs", true)));

        // ═══════════════════ DateTimeCore ═══════════════════
        Register("DateTimeCore", "IsoWeekNum", (a, _) => DateTimeCore.IsoWeekNum(ToDateTime(a[0])));
        Register("DateTimeCore", "Easter", (a, _) => DateTimeCore.Easter(ToLong(a[0])));
        Register("DateTimeCore", "IsLeapYear", (a, _) => DateTimeCore.IsLeapYear(ToLong(a[0])));
        Register("DateTimeCore", "AddWorkdays", (a, _) => DateTimeCore.AddWorkdays(ToDateTime(a[0]), ToLong(a[1])));
        Register("DateTimeCore", "NextWorkday", (a, _) => DateTimeCore.NextWorkday(ToDateTime(a[0])));

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

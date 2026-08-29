using System.Text.Json;
using System.Text.Json.Serialization;

namespace ExcelFormulaLabs.CrossValRunner;

/// <summary>
/// Converts C# Core method return values to JSON-serializable trees.
/// Handles: double, long, bool, string, double[], double[,], int[],
/// Dictionary&lt;string,object&gt;, ValueTuple, DateTime, double.NaN→null.
/// </summary>
public static class ResultSerializer
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = false,
        NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals,
    };

    /// <summary>Convert any Core method result to a JSON-serializable object.</summary>
    public static object? ToJsonFriendly(object? value)
    {
        return value switch
        {
            null => null,
            double d => double.IsNaN(d) || double.IsInfinity(d) ? null : d,
            long l => l,
            int i => i,
            bool b => b,
            string s => s,
            DateTime dt => dt.ToString("O"),
            double[] da => da.Select(d => (object?)(
                double.IsNaN(d) || double.IsInfinity(d) ? null : d)).ToArray(),
            string[] sa => sa.Cast<object>().ToArray(),
            int[] ia => ia.Cast<object>().ToArray(),
            long[] la => la.Cast<object>().ToArray(),
            double[,] d2 => MatrixToJagged(d2),
            object[,] o2 => ObjectMatrixToJagged(o2),
            object[] oa => oa.Select(ToJsonFriendly).ToArray(),
            Dictionary<string, object> dict => DictToObject(dict),
            _ => TryTupleToObject(value)
        };
    }

    private static double?[][] MatrixToJagged(double[,] m)
    {
        int rows = m.GetLength(0), cols = m.GetLength(1);
        var result = new double?[rows][];
        for (int r = 0; r < rows; r++)
        {
            result[r] = new double?[cols];
            for (int c = 0; c < cols; c++)
            {
                double v = m[r, c];
                result[r][c] = double.IsNaN(v) || double.IsInfinity(v) ? null : v;
            }
        }
        return result;
    }

    /// <summary>Convert an object[,] (e.g. DOE analysis tables with string header row)
    /// to a jagged JSON-friendly array, recursing per element.</summary>
    private static object?[][] ObjectMatrixToJagged(object[,] m)
    {
        int rows = m.GetLength(0), cols = m.GetLength(1);
        var result = new object?[rows][];
        for (int r = 0; r < rows; r++)
        {
            result[r] = new object?[cols];
            for (int c = 0; c < cols; c++)
                result[r][c] = ToJsonFriendly(m[r, c]);
        }
        return result;
    }

    private static Dictionary<string, object?> DictToObject(Dictionary<string, object> dict)
    {
        var result = new Dictionary<string, object?>();
        foreach (var kv in dict)
            result[kv.Key] = ToJsonFriendly(kv.Value);
        return result;
    }

    /// <summary>
    /// Attempt to extract named fields from a ValueTuple via reflection.
    /// Falls back to Item1, Item2, ... if no named fields found.
    /// </summary>
    private static object? TryTupleToObject(object value)
    {
        var type = value.GetType();
        if (!type.IsValueType || !type.FullName!.StartsWith("System.ValueTuple"))
            return value.ToString();

        var dict = new Dictionary<string, object?>();
        var fields = type.GetFields();
        int idx = 1;
        foreach (var field in fields)
        {
            var name = field.Name;
            if (name.StartsWith("Item") && name.Length > 4 && int.TryParse(name[4..], out _))
                name = $"Item{idx}";
            dict[name] = ToJsonFriendly(field.GetValue(value));
            idx++;
        }
        return dict;
    }

    /// <summary>Serialize the complete result root to JSON string.</summary>
    public static string Serialize(ResultsRoot root)
    {
        return JsonSerializer.Serialize(root, JsonOpts);
    }
}

public class ResultsRoot
{
    [JsonPropertyName("runner")]
    public string Runner { get; set; } = "CrossValRunner";

    [JsonPropertyName("tf")]
    public string Tf { get; set; } = "net8.0-windows";

    [JsonPropertyName("timestamp")]
    public string Timestamp { get; set; } = DateTime.UtcNow.ToString("O");

    [JsonPropertyName("results")]
    public List<TestResult> Results { get; set; } = new();

    [JsonPropertyName("summary")]
    public ResultSummary Summary { get; set; } = new();
}

public class TestResult
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("module")]
    public string Module { get; set; } = "";

    [JsonPropertyName("status")]
    public string Status { get; set; } = "ok";

    [JsonPropertyName("result")]
    public object? Result { get; set; }

    [JsonPropertyName("tolerance")]
    public double? Tolerance { get; set; }

    [JsonPropertyName("error")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Error { get; set; }
}

public class ResultSummary
{
    [JsonPropertyName("total")]
    public int Total { get; set; }

    [JsonPropertyName("ok")]
    public int Ok { get; set; }

    [JsonPropertyName("error")]
    public int Error { get; set; }
}

using System.Text.Json.Serialization;

namespace ExcelFormulaLabs.CrossValRunner;

/// <summary>Top-level manifest structure loaded from test_manifest.json.</summary>
public class ManifestRoot
{
    [JsonPropertyName("sharedData")]
    public Dictionary<string, object?> SharedData { get; set; } = new();

    [JsonPropertyName("tests")]
    public List<TestCase> Tests { get; set; } = new();
}

/// <summary>Single test case: maps to one Core method call with known arguments.</summary>
public class TestCase
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("module")]
    public string Module { get; set; } = "";

    [JsonPropertyName("coreClass")]
    public string CoreClass { get; set; } = "";

    [JsonPropertyName("coreMethod")]
    public string CoreMethod { get; set; } = "";

    /// <summary>Positional arguments. Each element is either a literal or a {"ref":"key"}.</summary>
    [JsonPropertyName("args")]
    public List<object?> Args { get; set; } = new();

    /// <summary>Named optional parameters (e.g. {"tUnit":"K", "addIntercept":false}).</summary>
    [JsonPropertyName("kwargs")]
    public Dictionary<string, object?>? Kwargs { get; set; }

    [JsonPropertyName("tolerance")]
    public double? Tolerance { get; set; }
}

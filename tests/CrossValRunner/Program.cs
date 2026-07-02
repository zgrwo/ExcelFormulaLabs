using System.Text.Json;
using ExcelFormulaLabs.CrossValRunner;

// ── Load manifest ──
string manifestPath = args.Length > 0 ? args[0] : "test_manifest.json";
if (!File.Exists(manifestPath))
{
    Console.Error.WriteLine($"Manifest not found: {manifestPath}");
    return 1;
}
var manifest = JsonSerializer.Deserialize<ManifestRoot>(
    File.ReadAllText(manifestPath),
    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
if (manifest == null) { Console.Error.WriteLine("Failed to parse manifest."); return 1; }

// ── Resolve shared data refs ──
object? ResolveArg(object? arg)
{
    if (arg is JsonElement je && je.ValueKind == JsonValueKind.Object &&
        je.TryGetProperty("ref", out var refProp))
    {
        string key = refProp.GetString() ?? "";
        if (manifest.SharedData.TryGetValue(key, out var shared)) return shared;
        Console.Error.WriteLine($"WARNING: shared data '{key}' not found.");
        return null;
    }
    return arg;
}

// ── Run tests ──
var results = new ResultsRoot();
foreach (var tc in manifest.Tests)
{
    var resolvedArgs = tc.Args.Select(ResolveArg).ToArray();
    var (result, error) = Dispatcher.Invoke(tc.CoreClass, tc.CoreMethod,
        resolvedArgs!, tc.Kwargs);
    results.Results.Add(new TestResult
    {
        Id = tc.Id, Module = tc.Module,
        Status = error == null ? "ok" : "error",
        Result = error == null ? ResultSerializer.ToJsonFriendly(result) : null,
        Tolerance = tc.Tolerance, Error = error
    });
}
results.Summary = new ResultSummary
{
    Total = results.Results.Count,
    Ok = results.Results.Count(r => r.Status == "ok"),
    Error = results.Results.Count(r => r.Status != "ok")
};

Console.WriteLine(ResultSerializer.Serialize(results));
return 0;

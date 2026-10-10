using System.Globalization;
using System.Text.Json;
using ExcelFormulaLabs.CrossValRunner;

// 2026-10-10 审查 E-2：本宿主是 Python↔C# 交叉验证的 C# 半边，而 3 个 xUnit 工程都有
// TestCultureSetup.cs 以 [ModuleInitializer] 固定 InvariantCulture——本进程此前没有任何固定，
// 于是在 tr-TR/de-DE 等默认 culture 的机器上，字符串型 UDF（STR.FORMAT、DT.* 格式化、
// RANGE.TOJSON/TOCSV）与 ResultSerializer 的输出会偏离，产生"环境归因"的间歇性 FAIL/SKIP。
// 必须在任何被测代码执行前固定（结果序列化同样依赖它）。
CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;

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

// FS 探针目录清理（Phase 4.5）——结果已收集，清理失败不影响输出。
Dispatcher.CleanupFsProbe();

Console.WriteLine(ResultSerializer.Serialize(results));
return 0;

# ============================================================================
# test_precommit_check.ps1 — pre-commit-check.ps1 回归守卫
# 场景：17 个 fixture，逐一验证 6 项检查的检测能力（含自校验/hasHeaders 检测：
#       跨行调用、短别名、元组参数、泛型委托、二层元组/NRT/修饰符链、豁免名单不误报）。
#       [17] 为检查 4 新作用域（白名单豁免 + 全量扫描）的配对守卫：非 *Core 文件引用
#       ExcelDna 必须被检出——旧 `*Core.cs` 作用域下该夹具 exit 0 漏检。
# 用法：pwsh 或 powershell 均可 -NoProfile -ExecutionPolicy Bypass -File tests/scripts/test_precommit_check.ps1
# 被测门禁经 $hostCmd 优先 pwsh7 调用（恒用 powershell 会使 pwsh7 语义差异永不暴露）；
#       与 run-tests.ps1 的宿主策略一致。
# ============================================================================
$ErrorActionPreference = "Stop"
$repo = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)   # 仓库根
$checker = Join-Path $repo "scripts\pre-commit-check.ps1"
$tmpRoot = Join-Path $env:TEMP ("pcc-test-" + [guid]::NewGuid().ToString("N"))
New-Item -ItemType Directory -Path $tmpRoot -Force | Out-Null

$passCount = 0; $failCount = 0
$hostCmd = if (Get-Command pwsh -ErrorAction SilentlyContinue) { "pwsh" } else { "powershell" }

function New-Fixture {
    param([string]$Name, [hashtable]$Files)
    $dir = Join-Path $tmpRoot $Name
    foreach ($rel in $Files.Keys) {
        $p = Join-Path $dir $rel
        New-Item -ItemType Directory -Path (Split-Path -Parent $p) -Force | Out-Null
        [System.IO.File]::WriteAllText($p, $Files[$rel], (New-Object System.Text.UTF8Encoding($false)))
    }
    return $dir
}

function Run-Check {
    param([string]$Dir, [string]$ExpectCode, [bool]$ExpectFail = $true)
    $output = & $hostCmd -NoProfile -ExecutionPolicy Bypass -File $checker -RepoRoot $Dir 2>&1
    $exit = $LASTEXITCODE
    $out = ($output | Out-String)
    $ok = $false
    if ($ExpectFail) {
        $ok = ($exit -ne 0) -and ($out -match [regex]::Escape($ExpectCode))
    } else {
        $ok = ($exit -eq 0)
    }
    if ($ok) {
        $script:passCount++
        Write-Host "  [PASS] $ExpectCode (exit=$exit)" -ForegroundColor Green
    } else {
        $script:failCount++
        Write-Host "  [FAIL] $ExpectCode (exit=$exit, 期望输出含 '$ExpectCode')" -ForegroundColor Red
        Write-Host ($out | Select-Object -Last 5 | ForEach-Object { "      $_" })
    }
}

# --- 场景 1：干净树 → 全部通过 ---
Write-Host "[1/17] 干净树应全部通过"
$clean = New-Fixture "clean" @{
    "src\StatsCore.cs" = @"
internal static class StatsCore {
    internal static double Mean(double[] x) {
        if (x.Length == 0) return double.NaN;
        double s = 0;
        foreach (var v in x) { s += v / x.Length; }
        return s;
    }
}
"@
    "scripts\verify-manual.py" = "print('ok')`n"
}
Run-Check $clean "" $false

# --- 场景 2：裸 catch ---
Write-Host "[2/17] 裸 catch 应被检出 (BARE_CATCH)"
$c2 = New-Fixture "barecatch" @{ "src\bad.cs" = "class Bad { void M() { try { } catch { } } }`n" }
Run-Check $c2 "BARE_CATCH"

# --- 场景 3：自校验（含嵌套调用/数组参数——修复后的括号平衡解析必须检出）---
Write-Host "[3/17] 自校验应被检出 (SELF_CHECK)"
$c3 = New-Fixture "selfcheck" @{
    "scripts\verify-manual.py" = @"
check("m", stats.mean(x), stats.mean(x))
check("t", np.array([1, 2]), np.array([1, 2]))
"@
}
Run-Check $c3 "SELF_CHECK"

# --- 场景 4：net8.0 IntelliSense 泄漏 ---
Write-Host "[4/17] IntelliSense 泄漏应被检出 (INTELLISENSE_LEAK)"
$c4 = New-Fixture "intelli" @{ "src\foo.cs" = "class Foo { void M() { var x = ExcelDna.IntelliSense.Thing; } }`n" }
Run-Check $c4 "INTELLISENSE_LEAK"

# --- 场景 5：Core 层引用 ExcelDna ---
Write-Host "[5/17] Core 层 ExcelDna 引用应被检出 (CORE_EXCEL_REF)"
$c5 = New-Fixture "coreref" @{ "src\EvilCore.cs" = "using ExcelDna.Integration;`nclass EvilCore { }`n" }
Run-Check $c5 "CORE_EXCEL_REF"

# --- 场景 6：除法无 NaN/Inf 守卫 ---
Write-Host "[6/17] 除法无守卫应被检出 (NAN_INF_GUARD)"
$c6 = New-Fixture "nanguard" @{ "src\StatsCore.cs" = "internal static class StatsCore { internal static double R(double a, double b) { return a / b; } }`n" }
Run-Check $c6 "NAN_INF_GUARD"

# --- 场景 7：object[,] 无 hasHeaders ---
Write-Host "[7/17] object[,] 无 hasHeaders 应被检出 (HAS_HEADERS)"
$c7 = New-Fixture "headers" @{ "src\TableCore.cs" = "internal static class TableCore { internal static object[] Foo(object[,] data) { return null; } }`n" }
Run-Check $c7 "HAS_HEADERS"

# --- 场景 8：跨行自校验（全文扫描必须检出跨行 check(，单行解析会绕过）---
Write-Host "[8/17] 跨行自校验应被检出 (SELF_CHECK)"
$c8 = New-Fixture "selfcheck-multiline" @{
    "scripts\verify-manual.py" = @"
check("m",
    stats.mean(x),
    stats.mean(x))
"@
}
Run-Check $c8 "SELF_CHECK"

# --- 场景 9：短别名自校验（无长度豁免，check("m", x, x) 必须检出）---
Write-Host "[9/17] 短别名自校验应被检出 (SELF_CHECK)"
$c9 = New-Fixture "selfcheck-alias" @{
    "scripts\verify-manual.py" = @"
x = [1, 2, 3]
check("m", x, x)
"@
}
Run-Check $c9 "SELF_CHECK"

# --- 场景 10：元组参数含 object[,]（一层嵌套括号提取，[^)]* 正则会漏报）---
Write-Host "[10/17] 元组参数 object[,] 无 hasHeaders 应被检出 (HAS_HEADERS)"
$c10 = New-Fixture "tuple-headers" @{ "src\TableCore.cs" = "internal static class TableCore { internal static void Join((int,int) key, object[,] data) { } }`n" }
Run-Check $c10 "HAS_HEADERS"

# --- 场景 11：泛型委托参数 object[,]（Func<object[,],bool> 必须命中）---
Write-Host "[11/17] 泛型 Func<object[,],bool> 无 hasHeaders 应被检出 (HAS_HEADERS)"
$c11 = New-Fixture "generic-headers" @{ "src\TableCore.cs" = "internal static class TableCore { internal static void Map(Func<object[,],bool> f) { } }`n" }
Run-Check $c11 "HAS_HEADERS"

# --- 场景 12：二层元组参数（一层嵌套正则会漏报，fixture 实测 exit=0）---
Write-Host "[12/17] 二层元组参数 object[,] 无 hasHeaders 应被检出 (HAS_HEADERS)"
$c12 = New-Fixture "tuple2-headers" @{ "src\TableCore.cs" = "internal static class TableCore { internal static void Join((int,(int,string)) t, object[,] data) { } }`n" }
Run-Check $c12 "HAS_HEADERS"

# --- 场景 13：NRT 注解 + 修饰符链 + protected（object?[,-] 与 protected 会漏报）---
Write-Host "[13/17] NRT object?[,] + 修饰符链 + protected 应被检出 (HAS_HEADERS)"
$c13 = New-Fixture "nrt-headers" @{ "src\TableCore.cs" = "internal static class TableCore { protected internal static object[,] Pivot(object?[,] data, (int,(int,string)) t) { return data!; } }`n" }
Run-Check $c13 "HAS_HEADERS"

# --- 场景 14：豁免名单与 private 不误报（结构性豁免/private 不产生违例）---
Write-Host "[14/17] 豁免名单方法与 private helper 不应误报"
$c14 = New-Fixture "exempt-ok" @{
    "src\TableCore.cs" = @"
internal static class TableCore {
    public static object[,] Transpose(object[,] data) => data;
    public static object[,] SelectColumns(object[,] data, object idx) => data;
    private static object[,] Helper(object[,] data) => data;
}
"@
}
Run-Check $c14 "" $false

# --- 场景 15：切片自校验 check(name, a, a[:])（R3-18：文本不同但同源）---
Write-Host "[15/17] 切片自校验 check(name, a, a[:]) 应被检出 (SELF_CHECK)"
$c15 = New-Fixture "selfcheck-slice" @{
    "scripts/verify-manual.py" = @"
check("m", arr, arr[:])
"@
}
Run-Check $c15 "SELF_CHECK"

# --- 场景 16：空白/尾逗号调用自校验 check(name, f(x), f( x ,))（归一化后同源）---
Write-Host "[16/17] 空白/尾逗号调用自校验应被检出 (SELF_CHECK)"
$c16 = New-Fixture "selfcheck-ws" @{
    "scripts/verify-manual.py" = @"
check("m", stats.mean( x ), stats.mean(x,))
"@
}
Run-Check $c16 "SELF_CHECK"

# --- 场景 17（P3-7）：配对夹具——Core 层文件引用 ExcelDna 但**文件名不含 Core** ---
# 场景 5 的 EvilCore.cs 在**旧作用域**（`-Filter "*Core.cs"`）下同样会被扫到，故它无法证明
# 5275f7b 把检查 4 的作用域扩为「白名单豁免 + 全量扫描」。本场景给出一对同名夹具：
#   (a) src\AnalyticsHelpers.cs 无 ExcelDna 引用 → exit 0（证明夹具本身干净，不靠其他检查兜底）；
#   (b) 同一文件加 `using ExcelDna.Integration;` → 检查 4 必须 FAIL。
# 实测（负向）：把检查 4 临时改回 `-Filter "*Core.cs"` 旧作用域后，(b) 输出
#   `[OK] Core layer has zero Excel dependency (scanned 0/1 files)` exit 0 → 本场景 [FAIL]；
# 恢复新作用域后 [PASS]。即本场景是"新旧作用域可分"的唯一守卫。
Write-Host "[17/17] 非 *Core 文件的 ExcelDna 引用应被检出 (CORE_EXCEL_REF)"
$c17a = New-Fixture "adapter-scope-clean" @{
    "src\AnalyticsHelpers.cs" = "internal static class AnalyticsHelpers { internal static double Id(double a) { return a; } }`n"
}
Run-Check $c17a "" $false
$c17b = New-Fixture "adapter-scope" @{
    "src\AnalyticsHelpers.cs" = "using ExcelDna.Integration;`n`ninternal static class AnalyticsHelpers { internal static double Id(double a) { return a; } }`n"
}
Run-Check $c17b "CORE_EXCEL_REF"

# --- 汇总 ---
Remove-Item -Recurse -Force $tmpRoot
Write-Host ""
Write-Host "=== Pass: $passCount  Fail: $failCount ==="
if ($failCount -gt 0) { exit 1 } else { exit 0 }

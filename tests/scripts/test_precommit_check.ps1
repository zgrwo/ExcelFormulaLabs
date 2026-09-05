# ============================================================================
# test_precommit_check.ps1 — pre-commit-check.ps1 回归守卫
# 场景：11 个 fixture，逐一验证 6 项检查的检测能力（含修复后的自校验/hasHeaders 检测：
#       跨行调用、短别名、元组参数、泛型委托——R15/R18 review-2026-09-05）。
# 用法：powershell -NoProfile -ExecutionPolicy Bypass -File tests/scripts/test_precommit_check.ps1
# ============================================================================
$ErrorActionPreference = "Stop"
$repo = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)   # 仓库根
$checker = Join-Path $repo "scripts\pre-commit-check.ps1"
$tmpRoot = Join-Path $env:TEMP ("pcc-test-" + [guid]::NewGuid().ToString("N"))
New-Item -ItemType Directory -Path $tmpRoot -Force | Out-Null

$passCount = 0; $failCount = 0

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
    $output = & powershell -NoProfile -ExecutionPolicy Bypass -File $checker -RepoRoot $Dir 2>&1
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
Write-Host "[1/11] 干净树应全部通过"
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
Write-Host "[2/11] 裸 catch 应被检出 (BARE_CATCH)"
$c2 = New-Fixture "barecatch" @{ "src\bad.cs" = "class Bad { void M() { try { } catch { } } }`n" }
Run-Check $c2 "BARE_CATCH"

# --- 场景 3：自校验（含嵌套调用/数组参数——修复后的括号平衡解析必须检出）---
Write-Host "[3/11] 自校验应被检出 (SELF_CHECK)"
$c3 = New-Fixture "selfcheck" @{
    "scripts\verify-manual.py" = @"
check("m", stats.mean(x), stats.mean(x))
check("t", np.array([1, 2]), np.array([1, 2]))
"@
}
Run-Check $c3 "SELF_CHECK"

# --- 场景 4：net8.0 IntelliSense 泄漏 ---
Write-Host "[4/11] IntelliSense 泄漏应被检出 (INTELLISENSE_LEAK)"
$c4 = New-Fixture "intelli" @{ "src\foo.cs" = "class Foo { void M() { var x = ExcelDna.IntelliSense.Thing; } }`n" }
Run-Check $c4 "INTELLISENSE_LEAK"

# --- 场景 5：Core 层引用 ExcelDna ---
Write-Host "[5/11] Core 层 ExcelDna 引用应被检出 (CORE_EXCEL_REF)"
$c5 = New-Fixture "coreref" @{ "src\EvilCore.cs" = "using ExcelDna.Integration;`nclass EvilCore { }`n" }
Run-Check $c5 "CORE_EXCEL_REF"

# --- 场景 6：除法无 NaN/Inf 守卫 ---
Write-Host "[6/11] 除法无守卫应被检出 (NAN_INF_GUARD)"
$c6 = New-Fixture "nanguard" @{ "src\StatsCore.cs" = "internal static class StatsCore { internal static double R(double a, double b) { return a / b; } }`n" }
Run-Check $c6 "NAN_INF_GUARD"

# --- 场景 7：object[,] 无 hasHeaders ---
Write-Host "[7/11] object[,] 无 hasHeaders 应被检出 (HAS_HEADERS)"
$c7 = New-Fixture "headers" @{ "src\TableCore.cs" = "internal static class TableCore { internal static object[] Foo(object[,] data) { return null; } }`n" }
Run-Check $c7 "HAS_HEADERS"

# --- 场景 8：跨行自校验（R15：全文扫描必须检出跨行 check(，单行解析曾绕过）---
Write-Host "[8/11] 跨行自校验应被检出 (SELF_CHECK)"
$c8 = New-Fixture "selfcheck-multiline" @{
    "scripts\verify-manual.py" = @"
check("m",
    stats.mean(x),
    stats.mean(x))
"@
}
Run-Check $c8 "SELF_CHECK"

# --- 场景 9：短别名自校验（R15：移除 Length>3 豁免，check("m", x, x) 必须检出）---
Write-Host "[9/11] 短别名自校验应被检出 (SELF_CHECK)"
$c9 = New-Fixture "selfcheck-alias" @{
    "scripts\verify-manual.py" = @"
x = [1, 2, 3]
check("m", x, x)
"@
}
Run-Check $c9 "SELF_CHECK"

# --- 场景 10：元组参数含 object[,]（R18：一层嵌套括号提取，原 [^)]* 正则漏报）---
Write-Host "[10/11] 元组参数 object[,] 无 hasHeaders 应被检出 (HAS_HEADERS)"
$c10 = New-Fixture "tuple-headers" @{ "src\TableCore.cs" = "internal static class TableCore { internal static void Join((int,int) key, object[,] data) { } }`n" }
Run-Check $c10 "HAS_HEADERS"

# --- 场景 11：泛型委托参数 object[,]（R18：Func<object[,],bool> 保持命中）---
Write-Host "[11/11] 泛型 Func<object[,],bool> 无 hasHeaders 应被检出 (HAS_HEADERS)"
$c11 = New-Fixture "generic-headers" @{ "src\TableCore.cs" = "internal static class TableCore { internal static void Map(Func<object[,],bool> f) { } }`n" }
Run-Check $c11 "HAS_HEADERS"

# --- 汇总 ---
Remove-Item -Recurse -Force $tmpRoot
Write-Host ""
Write-Host "=== Pass: $passCount  Fail: $failCount ==="
if ($failCount -gt 0) { exit 1 } else { exit 0 }

# ============================================================================
# test_check_test_quality.ps1 — check-test-quality.ps1 回归守卫
# 场景：8 组 fixture，验证零断言/恒真断言/存在性断言预算的检测能力，
#       以及表达式体、注释内分号、字符串内花括号、自定义 Assert* helper 不误报；
#       场景 8 覆盖声明形态逃逸（合并属性/全限定 Task/static/ValueTask/FactAttribute/Skip=）。
# 用法：pwsh 或 powershell 均可 -NoProfile -ExecutionPolicy Bypass -File tests/scripts/test_check_test_quality.ps1
# 被测门禁经 $hostCmd 优先 pwsh7 调用（与 run-tests.ps1 宿主策略一致）。
# ============================================================================
$ErrorActionPreference = "Stop"
$repo = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)   # 仓库根
$checker = Join-Path $repo "scripts\check-test-quality.ps1"
$tmpRoot = Join-Path $env:TEMP ("ctq-test-" + [guid]::NewGuid().ToString("N"))
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
    param([string]$Dir, [string]$ExpectCode, [bool]$ExpectFail = $true, [string[]]$ExtraArgs = @())
    $args = @("-NoProfile", "-ExecutionPolicy", "Bypass", "-File", $checker, "-RepoRoot", $Dir) + $ExtraArgs
    $output = & $hostCmd @args 2>&1
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
        Write-Host ($out | Select-Object -Last 6 | ForEach-Object { "      $_" })
    }
}

# --- 场景 1：干净树 → 通过（表达式体 + 块体 + 自定义 helper + 注释内分号/花括号）---
Write-Host "[1/8] 干净树应通过（含表达式体/helper/注释内分号不误报）"
$clean = New-Fixture "clean" @{
    "tests\SampleTests.cs" = @"
using FluentAssertions;
using Xunit;
public class SampleTests
{
    [Fact] public void Expr_ok() =>
        // comment with ; semicolon and { brace
        Sample.Mean(new[] { 1, 2, 3 }).Should().BeApproximately(2.0, 1e-10);

    [Fact] public void Block_ok()
    {
        var s = "} ; not code";
        Sample.Mean(new[] { 1.0, 2.0 }).Should().Be(1.5);
    }

    [Fact] public void Helper_ok() => AssertShape(Sample.Mean(new[] { 1.0 }));

    private static void AssertShape(double v) { v.Should().Be(1.0); }
}
"@
}
Run-Check $clean "" $false

# --- 场景 2：零断言应被检出 (ZERO_ASSERT) ---
Write-Host "[2/8] 零断言应被检出 (ZERO_ASSERT)"
$zero = New-Fixture "zero" @{
    "tests\ZeroTests.cs" = @"
using Xunit;
public class ZeroTests
{
    [Fact] public void Does_nothing() { Sample.Mean(new[] { 1.0 }); }
}
"@
}
Run-Check $zero "ZERO_ASSERT"

# --- 场景 3：恒真断言应被检出 (TAUTOLOGY) ---
Write-Host "[3/8] 恒真断言应被检出 (TAUTOLOGY)"
$taut = New-Fixture "taut" @{
    "tests\TautTests.cs" = @"
using Xunit;
public class TautTests
{
    [Fact] public void Always_true() { Assert.True(true); }
}
"@
}
Run-Check $taut "TAUTOLOGY"

# --- 场景 4：存在性断言超预算应被检出 (PRESENCE_ONLY) ---
Write-Host "[4/8] 存在性断言超预算应被检出 (PRESENCE_ONLY)"
$presence = New-Fixture "presence" @{
    "tests\PresenceTests.cs" = @"
using FluentAssertions;
using Xunit;
public class PresenceTests
{
    [Fact] public void Only_not_null() => Sample.Uuid().Should().NotBeNull();
}
"@
}
Run-Check $presence "PRESENCE_ONLY" $true @("-MaxWarn", "0")

# --- 场景 5：存在性断言在预算内 → 通过 ---
Write-Host "[5/8] 存在性断言在预算内应通过"
Run-Check $presence "" $false @("-MaxWarn", "1")

# --- 场景 6：注释掉的断言不算断言（防注释规避门禁）---
Write-Host "[6/8] 注释内 Should() 不得冒充断言 (ZERO_ASSERT)"
$commented = New-Fixture "commented" @{
    "tests\CommentedTests.cs" = @"
using Xunit;
public class CommentedTests
{
    [Fact] public void Fake() { Sample.Mean(new[] { 1.0 }); /* .Should().Be(1.0); */ }
}
"@
}
Run-Check $commented "ZERO_ASSERT"

# --- 场景 7：真实仓库 → 全绿（正向集成）---
Write-Host "[7/8] 真实仓库应全绿"
Run-Check $repo "" $false

# --- 场景 8：R1-10/F-08 声明形态逃逸（合并属性/全限定 Task/static/ValueTask/FactAttribute）---
# 每形态独立 fixture 并以方法名为期望输出——四种形态若任一种被正则漏掉，对应 Run-Check 失败。
Write-Host "[8/8] 声明形态变体的零断言测试必须被检出 (ZERO_ASSERT)"
$formCombined = New-Fixture "form-combined" @{
    "tests\FormCombined.cs" = @"
using Xunit;
public class FormCombined
{
    [Fact, Trait("Category", "Security")] public void Combined_attribute() { Sample.Mean(new[] { 1.0 }); }
}
"@
}
Run-Check $formCombined "Combined_attribute"

$formQualified = New-Fixture "form-qualified" @{
    "tests\FormQualified.cs" = @"
using Xunit;
public class FormQualified
{
    [Fact] public async System.Threading.Tasks.Task Fully_qualified() { await Sample.DoAsync(); }
}
"@
}
Run-Check $formQualified "Fully_qualified"

$formStatic = New-Fixture "form-static" @{
    "tests\FormStatic.cs" = @"
using Xunit;
public class FormStatic
{
    [FactAttribute] public static void Attr_suffix_static() { Sample.Mean(new[] { 1.0 }); }
}
"@
}
Run-Check $formStatic "Attr_suffix_static"

$formValueTask = New-Fixture "form-valuetask" @{
    "tests\FormValueTask.cs" = @"
using Xunit;
public class FormValueTask
{
    [Fact] public System.Threading.Tasks.ValueTask Value_task() { Sample.Mean(new[] { 1.0 }); return default; }
}
"@
}
Run-Check $formValueTask "Value_task"

$formSkip = New-Fixture "form-skip" @{
    "tests\FormSkip.cs" = @"
using Xunit;
public class FormSkip
{
    [Fact(Skip = "temporarily disabled")] public void Skipped_zero_assert() { Sample.Mean(new[] { 1.0 }); }
}
"@
}
Run-Check $formSkip "Skipped_zero_assert"

$formComment = New-Fixture "form-comment" @{
    "tests\FormComment.cs" = @"
using Xunit;
public class FormComment
{
    [Fact]
    // comment between attribute and signature
    public void Comment_between() { Sample.Mean(new[] { 1.0 }); }
}
"@
}
Run-Check $formComment "Comment_between"

# --- 汇总 ---
Remove-Item -Recurse -Force $tmpRoot
Write-Host ""
Write-Host "=== Pass: $passCount  Fail: $failCount ==="
if ($failCount -gt 0) { exit 1 } else { exit 0 }

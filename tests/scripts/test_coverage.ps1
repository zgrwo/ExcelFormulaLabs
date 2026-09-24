# ============================================================================
# test_coverage.ps1 — coverage.ps1 回归守卫（R1-07 假绿）
# 场景：覆盖"测试失败但存在旧 cobertura 报告 → 旧脚本读旧报告报 PASS"的假绿路径：
#   [1] 陈旧通过报告 + 测试失败（项目不存在）→ 必须 FAIL，且旧报告被运行前清理
#   [2] 无报告 + 测试失败 → 必须 FAIL（report not found / dotnet test exit）
# 断言点：exit != 0、输出不得出现 "All coverage gates passed"、旧报告文件被删除。
# 用法：pwsh 或 powershell 均可 -NoProfile -ExecutionPolicy Bypass -File tests/scripts/test_coverage.ps1
# ============================================================================
$ErrorActionPreference = "Stop"
$repo = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)   # 仓库根
$coverageScript = Join-Path $repo "scripts\coverage.ps1"
$tmpRoot = Join-Path $env:TEMP ("coverage-test-" + [guid]::NewGuid().ToString("N"))
New-Item -ItemType Directory -Path $tmpRoot -Force | Out-Null

$passCount = 0; $failCount = 0
$hostCmd = if (Get-Command pwsh -ErrorAction SilentlyContinue) { "pwsh" } else { "powershell" }

function Assert-Scenario {
    param([string]$Name, [bool]$Ok, [string]$Detail = "")
    if ($Ok) {
        $script:passCount++
        Write-Host "  [PASS] $Name" -ForegroundColor Green
    } else {
        $script:failCount++
        Write-Host "  [FAIL] $Name $Detail" -ForegroundColor Red
    }
}

function New-StaleReport {
    param([string]$Fixture, [string]$ModuleDir, [string]$ReportName)
    $dir = Join-Path $Fixture "tests\$ModuleDir\coverage"
    New-Item -ItemType Directory -Path $dir -Force | Out-Null
    $xml = '<?xml version="1.0" encoding="utf-8"?><coverage line-rate="0.99" lines-covered="99" lines-valid="100"></coverage>'
    [System.IO.File]::WriteAllText((Join-Path $dir $ReportName), $xml, (New-Object System.Text.UTF8Encoding($false)))
}

function New-Fixture {
    param([string]$Name, [bool]$WithStaleReports)
    $fixture = Join-Path $tmpRoot $Name
    New-Item -ItemType Directory -Path $fixture -Force | Out-Null
    if ($WithStaleReports) {
        New-StaleReport $fixture "Foundation.Tests" "foundation.cobertura.xml"
        New-StaleReport $fixture "Analytics.Tests" "analytics.cobertura.xml"
        New-StaleReport $fixture "DataToolkit.Tests" "datatoolkit.cobertura.xml"
    }
    return $fixture
}

function Invoke-Coverage {
    param([string]$Fixture)
    $output = & $hostCmd -NoProfile -ExecutionPolicy Bypass -File $coverageScript -RepoRoot $Fixture 2>&1
    return [pscustomobject]@{ Exit = $LASTEXITCODE; Out = ($output | Out-String) }
}

Write-Host ""
Write-Host "=== [1] 陈旧通过报告 + 测试失败 → 必须 FAIL 且清理旧报告 ===" -ForegroundColor Cyan
$fixture1 = New-Fixture "stale" $true
$r1 = Invoke-Coverage $fixture1
$staleCleaned = -not (Test-Path (Join-Path $fixture1 "tests\Foundation.Tests\coverage\foundation.cobertura.xml"))
Assert-Scenario "stale-report false green blocked (exit != 0)" ($r1.Exit -ne 0) "exit=$($r1.Exit)"
Assert-Scenario "no 'All coverage gates passed' claim" ($r1.Out -notmatch 'All coverage gates passed')
Assert-Scenario "stale report removed before run" $staleCleaned

Write-Host ""
Write-Host "=== [2] 无报告 + 测试失败 → 必须 FAIL ===" -ForegroundColor Cyan
$fixture2 = New-Fixture "noreport" $false
$r2 = Invoke-Coverage $fixture2
Assert-Scenario "missing report blocked (exit != 0)" ($r2.Exit -ne 0) "exit=$($r2.Exit)"
Assert-Scenario "failure reason reported" (($r2.Out -match 'dotnet test exit') -or ($r2.Out -match 'report not found')) `
    "out=$($r2.Out.Substring(0, [Math]::Min(200, $r2.Out.Length)))"

# ── 汇总 ──
Write-Host ""
Write-Host "=== Pass: $passCount  Fail: $failCount ==="
Remove-Item -Path $tmpRoot -Recurse -Force -ErrorAction SilentlyContinue
if ($failCount -gt 0) { exit 1 } else { exit 0 }

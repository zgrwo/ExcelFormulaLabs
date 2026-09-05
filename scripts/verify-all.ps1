# verify-all.ps1 - One-command local verification (6-step gate)
# Usage: .\scripts\verify-all.ps1 [-Configuration Release] [-SkipCrossVal]
# Runs all verification steps required before a PR or release.

param(
    [string]$Configuration = "Debug",
    [switch]$SkipCrossVal,
    [switch]$SkipManual
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$sw = [System.Diagnostics.Stopwatch]::StartNew()
$failures = @()

function Step {
    param([string]$Label, [scriptblock]$Block)
    Write-Host ""
    Write-Host "=== $Label ==="
    try {
        & $Block
        if ($LASTEXITCODE -ne 0) { throw "Exit code $LASTEXITCODE" }
        Write-Host "[PASS] $Label"
    } catch {
        Write-Host "[FAIL] $Label - $_"
        $script:failures += $Label
    }
}

Write-Host "============================================"
Write-Host " ExcelFormulaLabs - Full Verification Gate"
Write-Host " Config: $Configuration"
Write-Host "============================================"

# P1-17 (review-2026-08-31)：AGENTS.md 定义全量验证 5 步 = ① verify-docs ② dotnet test
# ③ CrossVal ④ verify-manual.py ⑤ Release build——原实现没有 verify-docs 步骤。插入为 Step 1。
# R26 (review-2026-09-05)：本脚本实际为 6 步（另含 Step 5 Pre-commit Checks），头注 5-step→6-step。
# Step 1: verify-docs（文档一致性 19 项）
Step "1/6 verify-docs" {
    powershell -NoProfile -File "$root\scripts\verify-docs.ps1"
}

# Step 2: Build
Step "2/6 Build ($Configuration)" {
    dotnet build "$root\ExcelFormulaLabs.sln" -c $Configuration --nologo -v q
}

# Step 3: Unit Tests (all TFMs: net8.0 + net8.0-windows + net48)
Step "3/6 Unit Tests (all TFMs)" {
    dotnet test "$root\ExcelFormulaLabs.sln" -c $Configuration --no-build --nologo -v q
}

# Step 4: CrossVal (C# CrossValRunner + Python verify-manual.py)
if (-not $SkipCrossVal) {
    Step "4/6 CrossVal (verify-manual.py)" {
        python "$root\scripts\verify-manual.py"
    }
} else {
    Write-Host ""
    Write-Host "=== 4/6 CrossVal [SKIPPED] ==="
}

# Step 5: Pre-commit checks (bare catch / self-validation / IntelliSense / Core isolation)
Step "5/6 Pre-commit Checks" {
    powershell -NoProfile -File "$root\scripts\pre-commit-check.ps1"
}

# Step 6: Release build (dual TFM packaging verification)
Step "6/6 Release Build" {
    dotnet build "$root\ExcelFormulaLabs.sln" -c Release --nologo -v q
}

# Summary
$sw.Stop()
Write-Host ""
Write-Host "============================================"
if ($failures.Count -eq 0) {
    Write-Host " RESULT: ALL PASS ($([math]::Round($sw.Elapsed.TotalSeconds, 1))s)"
} else {
    Write-Host " RESULT: $($failures.Count) FAILURE(S)"
    foreach ($f in $failures) { Write-Host "   - $f" }
}
Write-Host "============================================"
exit $failures.Count

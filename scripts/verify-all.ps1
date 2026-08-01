# verify-all.ps1 - One-command local verification (5-step gate)
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

# Step 1: Build
Step "1/5 Build ($Configuration)" {
    dotnet build "$root\ExcelFormulaLabs.sln" -c $Configuration --nologo -v q
}

# Step 2: Unit Tests (all TFMs: net8.0 + net8.0-windows + net48)
Step "2/5 Unit Tests (all TFMs)" {
    dotnet test "$root\ExcelFormulaLabs.sln" -c $Configuration --no-build --nologo -v q
}

# Step 3: CrossVal (C# CrossValRunner + Python verify-manual.py)
if (-not $SkipCrossVal) {
    Step "3/5 CrossVal (verify-manual.py)" {
        python "$root\scripts\verify-manual.py"
    }
} else {
    Write-Host ""
    Write-Host "=== 3/5 CrossVal [SKIPPED] ==="
}

# Step 4: Pre-commit checks (bare catch / self-validation / IntelliSense / Core isolation)
Step "4/5 Pre-commit Checks" {
    powershell -NoProfile -File "$root\scripts\pre-commit-check.ps1"
}

# Step 5: Release build (dual TFM packaging verification)
Step "5/5 Release Build" {
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

#Requires -Version 5.1
<#
.SYNOPSIS
    ExcelFormulaLabs pre-commit check script
.DESCRIPTION
    Blocks commits with common violations:
    1. Bare catch {} - red line
    2. Self-validation check(name, X, X) - false negative
    3. IntelliSense code in net8.0 - framework isolation
    4. Core layer referencing ExcelDna - architecture violation
.NOTES
    Usage: .\scripts\pre-commit-check.ps1
#>

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
$violations = @()

Write-Host "============================================================"
Write-Host "  ExcelFormulaLabs Pre-Commit Check"
Write-Host "============================================================"

# -- Check 1: Bare catch {} --
Write-Host ""
Write-Host "[1/4] Checking bare catch {} ..."

$bareCatch = Get-ChildItem -Path "$repoRoot/src" -Recurse -Filter "*.cs" |
    Select-String -Pattern "catch\s*\{" -AllMatches

if ($bareCatch) {
    foreach ($m in $bareCatch) {
        $violations += "BARE_CATCH: $($m.Path):$($m.LineNumber)"
    }
    Write-Host "  [FAIL] Found $($bareCatch.Count) bare catch" -ForegroundColor Red
} else {
    Write-Host "  [OK] No bare catch" -ForegroundColor Green
}

# -- Check 2: Self-validation pattern --
Write-Host ""
Write-Host "[2/4] Checking self-validation pattern ..."

$verifyScript = Join-Path $repoRoot "scripts\verify-manual.py"
if (Test-Path $verifyScript) {
    $lines = Get-Content $verifyScript
    for ($i = 0; $i -lt $lines.Count; $i++) {
        if ($lines[$i] -match "check\(") {
            # Extract 2nd and 3rd args - simple heuristic for same-expression
            if ($lines[$i] -match "check\([^,]+,\s*(.+?),\s*(.+?)\s*[,)]") {
                $arg2 = $Matches[1].Trim()
                $arg3 = $Matches[2].Trim()
                if ($arg2 -eq $arg3 -and $arg2.Length -gt 3) {
                    $violations += "SELF_CHECK: verify-manual.py:$($i+1)"
                }
            }
        }
    }
    $selfCount = ($violations | Where-Object { $_ -like "SELF_CHECK*" }).Count
    if ($selfCount -gt 0) {
        Write-Host "  [FAIL] Found $selfCount self-validation" -ForegroundColor Red
    } else {
        Write-Host "  [OK] No self-validation" -ForegroundColor Green
    }
} else {
    Write-Host "  [SKIP] verify-manual.py not found" -ForegroundColor DarkYellow
}

# -- Check 3: IntelliSense in net8.0 --
Write-Host ""
Write-Host "[3/4] Checking IntelliSense isolation ..."

$allCs = Get-ChildItem -Path "$repoRoot/src" -Recurse -Filter "*.cs"
$intelliHits = $allCs | Select-String -Pattern "ExcelDna\.IntelliSense"
$leaked = @()

foreach ($hit in $intelliHits) {
    $content = Get-Content $hit.Path
    $lineIdx = $hit.LineNumber - 1
    $inNet48 = $false
    $start = [Math]::Max(0, $lineIdx - 10)
    for ($i = $start; $i -le $lineIdx; $i++) {
        if ($content[$i] -match "#if\s+NET48") { $inNet48 = $true }
        if ($content[$i] -match "#endif") { $inNet48 = $false }
    }
    if (-not $inNet48) {
        $leaked += $hit
    }
}

if ($leaked.Count -gt 0) {
    foreach ($m in $leaked) {
        $violations += "INTELLISENSE_LEAK: $($m.Path):$($m.LineNumber)"
    }
    Write-Host "  [FAIL] Found $($leaked.Count) IntelliSense outside NET48" -ForegroundColor Red
} else {
    Write-Host "  [OK] IntelliSense isolation correct" -ForegroundColor Green
}

# -- Check 4: Core layer ExcelDna reference --
Write-Host ""
Write-Host "[4/4] Checking Core layer isolation ..."

$coreFiles = Get-ChildItem -Path "$repoRoot/src" -Recurse -Filter "*Core.cs"
$coreHits = $coreFiles | Select-String -Pattern "ExcelDna"

if ($coreHits) {
    foreach ($m in $coreHits) {
        $violations += "CORE_EXCEL_REF: $($m.Path):$($m.LineNumber)"
    }
    Write-Host "  [FAIL] Found $($coreHits.Count) ExcelDna refs in Core" -ForegroundColor Red
} else {
    Write-Host "  [OK] Core layer has zero Excel dependency" -ForegroundColor Green
}

# -- Summary --
Write-Host ""
Write-Host "============================================================"
if ($violations.Count -gt 0) {
    Write-Host "  [BLOCKED] $($violations.Count) violation(s) found:" -ForegroundColor Red
    Write-Host ""
    foreach ($v in $violations) {
        Write-Host "    - $v" -ForegroundColor Red
    }
    Write-Host ""
    Write-Host "  Fix violations before committing." -ForegroundColor Red
    Write-Host "============================================================"
    exit 1
} else {
    Write-Host "  [PASS] All checks passed. Safe to commit." -ForegroundColor Green
    Write-Host "============================================================"
    exit 0
}

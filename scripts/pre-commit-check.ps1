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
    5. NaN/Inf guard missing in Core files with division
    6. hasHeaders parameter missing for object[,] Core methods
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
Write-Host "[1/6] Checking bare catch {} ..."

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
Write-Host "[2/6] Checking self-validation pattern ..."

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
Write-Host "[3/6] Checking IntelliSense isolation ..."

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
Write-Host "[4/6] Checking Core layer isolation ..."

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

# -- Check 5: NaN/Inf guard in Core files --
Write-Host ""
Write-Host "[5/6] Checking NaN/Inf guards in Core files ..."

$coreModules = @("StatsCore", "LinalgCore", "RegressionCore", "PhyChemCore")
$nanInfMissing = @()

foreach ($mod in $coreModules) {
    $files = $allCs | Where-Object { $_.Name -eq "$mod.cs" }
    foreach ($f in $files) {
        $content = Get-Content $f.FullName -Raw
        $hasDivision = $content -match '/\s*(?!0\b)\w+'
        if ($hasDivision) {
            $hasGuard = ($content -match 'double\.IsNaN') -or
                        ($content -match 'double\.IsInfinity') -or
                        ($content -match 'ArgumentException') -or
                        ($content -match 'double\.NaN')
            if (-not $hasGuard) {
                $nanInfMissing += $f.FullName
                $violations += "NAN_INF_GUARD: $($f.FullName)"
            }
        }
    }
}

if ($nanInfMissing.Count -gt 0) {
    Write-Host "  [FAIL] Found $($nanInfMissing.Count) Core file(s) without NaN/Inf guard" -ForegroundColor Red
} else {
    Write-Host "  [OK] All Core files with division have NaN/Inf guards" -ForegroundColor Green
}

# -- Check 6: hasHeaders parameter for object[,] Core methods --
Write-Host ""
Write-Host "[6/6] Checking hasHeaders contract ..."

$allCoreCs = Get-ChildItem -Path "$repoRoot/src" -Recurse -Filter "*Core.cs"
$hasHeaderViolations = @()
# Structural transformation exemptions (don't interpret header semantics)
$structuralExempt = @('Transpose','SelectColumns','SelectRows','CrossJoin','Flatten2D','Count',
                       'Frequency','Dict','JsonToTable','XmlToTable','RegexCaptureGroups')

foreach ($f in $allCoreCs) {
    $content = Get-Content $f.FullName -Raw
    # Match method signatures with object[,] as PARAMETER (not return type)
    # Use broader pattern to include access modifiers
    $paramMatches = [regex]::Matches($content, '(private|internal|public)\s+(?:static\s+)?\S+\s+(\w+)\s*\([^)]*object\s*\[,\s*\][^)]*\)')
    foreach ($pm in $paramMatches) {
        $sig = $pm.Value
        $accessMod = $pm.Groups[1].Value
        $methodName = $pm.Groups[2].Value
        # Skip private helpers (not part of hasHeaders contract)
        if ($accessMod -eq 'private') { continue }
        # Skip structural transformation exemptions
        if ($structuralExempt -contains $methodName) { continue }
        # Skip if method already has hasHeaders
        if ($sig -match 'hasHeaders') { continue }
        $hasHeaderViolations += "$($f.FullName):$methodName"
        $violations += "HAS_HEADERS: $($f.FullName):$methodName"
    }
}

if ($hasHeaderViolations.Count -gt 0) {
    Write-Host "  [FAIL] Found $($hasHeaderViolations.Count) Core file(s) with object[,] but no hasHeaders" -ForegroundColor Red
} else {
    Write-Host "  [OK] All Core files with object[,] have hasHeaders parameter" -ForegroundColor Green
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

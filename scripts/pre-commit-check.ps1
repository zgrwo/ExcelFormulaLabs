#Requires -Version 5.1
<#
.SYNOPSIS
    ExcelFormulaLabs pre-commit check script
.DESCRIPTION
    Blocks commits with common violations:
    1. Bare catch {} - red line
    2. Self-validation check(name, X, X) - false negative（括号平衡解析，支持嵌套调用/数组参数）
    3. IntelliSense code in net8.0 - framework isolation
    4. Core layer referencing ExcelDna - architecture violation
    5. NaN/Inf guard missing in Core files with division
    6. hasHeaders parameter missing for object[,] Core methods
.NOTES
    Usage: .\scripts\pre-commit-check.ps1 [-RepoRoot <path>]
    自测：tests/scripts/test_precommit_check.ps1（回归守卫，CI 强制执行）
#>
param(
    [string]$RepoRoot = (Split-Path -Parent $PSScriptRoot)
)

$ErrorActionPreference = "Stop"
$violations = @()

function Read-Utf8Text {
    param([string]$Path)
    if (-not (Test-Path $Path)) { return $null }
    return [System.IO.File]::ReadAllText($Path, [System.Text.Encoding]::UTF8)
}

# 从一行中提取 check( 调用的顶层参数列表（括号/引号平衡，支持嵌套调用）
function Split-TopLevelArgs {
    param([string]$Line, [int]$StartIndex)
    $args = New-Object System.Collections.Generic.List[string]
    $depth = 1          # 已处于 check( 括号内：深度 1 为顶层参数层
    $inStr = $null      # 当前引号字符（' 或 "）
    $buf = New-Object System.Text.StringBuilder
    $i = $StartIndex
    while ($i -lt $Line.Length) {
        $ch = $Line[$i]
        if ($inStr) {
            if ($ch -eq $inStr) {
                if ($i + 1 -lt $Line.Length -and $Line[$i+1] -eq $inStr) { [void]$buf.Append($ch); $i++ }
                else { $inStr = $null }
            } elseif ($ch -eq '\' -and $inStr -eq '"') {
                [void]$buf.Append($ch)
                if ($i + 1 -lt $Line.Length) { [void]$buf.Append($Line[$i+1]); $i++ }
            } else { [void]$buf.Append($ch) }
        } else {
            if ($ch -eq '"') { $inStr = '"'; [void]$buf.Append($ch) }
            elseif ($ch -eq "'") { $inStr = "'"; [void]$buf.Append($ch) }
            elseif ($ch -eq '(') { $depth++; [void]$buf.Append($ch) }
            elseif ($ch -eq ')') {
                $depth--
                if ($depth -eq 0) { break }   # 最外层 check( 闭合，结束
                [void]$buf.Append($ch)
            }
            elseif ($ch -eq ',' -and $depth -eq 1) { $args.Add($buf.ToString()); [void]$buf.Clear() }
            else { [void]$buf.Append($ch) }
        }
        $i++
    }
    if ($buf.Length -gt 0) { $args.Add($buf.ToString()) }
    return $args
}

# -- Check 1: Bare catch {} --
Write-Host ""
Write-Host "[1/6] Checking bare catch {} ..."

$bareCatch = Get-ChildItem -Path "$RepoRoot/src" -Recurse -Filter "*.cs" -ErrorAction SilentlyContinue |
    Select-String -Pattern "catch\s*\{" -AllMatches

if ($bareCatch) {
    foreach ($m in $bareCatch) {
        $violations += "BARE_CATCH: $($m.Path):$($m.LineNumber)"
    }
    Write-Host "  [FAIL] Found $($bareCatch.Count) bare catch" -ForegroundColor Red
} else {
    Write-Host "  [OK] No bare catch" -ForegroundColor Green
}

# -- Check 2: Self-validation pattern (check(name, X, X)) --
Write-Host ""
Write-Host "[2/6] Checking self-validation pattern ..."

$verifyScript = Join-Path $RepoRoot "scripts\verify-manual.py"
if (Test-Path $verifyScript) {
    # @() 强制数组：单行文件时 Get-Content 返回标量 string，索引会得到 char
    $lines = @(Get-Content $verifyScript)
    $selfHits = @()
    for ($i = 0; $i -lt $lines.Count; $i++) {
        $line = $lines[$i]
        $idx = $line.IndexOf("check(")
        while ($idx -ge 0) {
            $args = Split-TopLevelArgs $line ($idx + 6)
            # check(name, X, X)：第 2、3 个顶层参数完全相同且非平凡长度
            if ($args.Count -ge 3) {
                $a2 = $args[1].Trim()
                $a3 = $args[2].Trim()
                if ($a2 -eq $a3 -and $a2.Length -gt 3) {
                    $selfHits += "verify-manual.py:$($i+1) ($a2)"
                    break
                }
            }
            $idx = $line.IndexOf("check(", $idx + 1)
        }
    }
    if ($selfHits.Count -gt 0) {
        foreach ($h in $selfHits) { $violations += "SELF_CHECK: $h" }
        Write-Host "  [FAIL] Found $($selfHits.Count) self-validation" -ForegroundColor Red
    } else {
        Write-Host "  [OK] No self-validation" -ForegroundColor Green
    }
} else {
    Write-Host "  [SKIP] verify-manual.py not found" -ForegroundColor DarkYellow
}

# -- Check 3: IntelliSense in net8.0 --
Write-Host ""
Write-Host "[3/6] Checking IntelliSense isolation ..."

$allCs = Get-ChildItem -Path "$RepoRoot/src" -Recurse -Filter "*.cs" -ErrorAction SilentlyContinue
$intelliHits = $allCs | Select-String -Pattern "ExcelDna\.IntelliSense"
$leaked = @()

foreach ($hit in $intelliHits) {
    $content = @(Get-Content $hit.Path)
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

$coreFiles = Get-ChildItem -Path "$RepoRoot/src" -Recurse -Filter "*Core.cs" -ErrorAction SilentlyContinue
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

$coreModules = @("StatsCore", "LinalgCore", "RegressionCore", "PhyChemCore", "PivotCore", "JsonXmlCore")  # P2 (review): DataToolkit division paths now gated too
$nanInfMissing = @()

foreach ($mod in $coreModules) {
    $files = $allCs | Where-Object { $_.Name -eq "$mod.cs" }
    foreach ($f in $files) {
        $content = Read-Utf8Text $f.FullName
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

$allCoreCs = Get-ChildItem -Path "$RepoRoot/src" -Recurse -Filter "*Core.cs" -ErrorAction SilentlyContinue
$hasHeaderViolations = @()
# Structural transformation exemptions (don't interpret header semantics)
$structuralExempt = @('Transpose','SelectColumns','SelectRows','CrossJoin','Flatten2D','Count',
                       'Frequency','Dict','JsonToTable','XmlToTable','RegexCaptureGroups')

foreach ($f in $allCoreCs) {
    $content = Read-Utf8Text $f.FullName
    # Match method signatures with object[,] as PARAMETER (not return type)
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
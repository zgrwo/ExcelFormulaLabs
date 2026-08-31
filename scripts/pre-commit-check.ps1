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
    Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' } |
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

$allCs = Get-ChildItem -Path "$RepoRoot/src" -Recurse -Filter "*.cs" -ErrorAction SilentlyContinue |
    Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' }
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

# review-2026-08-29 P2-6：原硬编码 $coreModules 名单缺 DoeCore/DoeAnalysisCore（DOE 新增后未扩展）
# → 改为动态发现全部 *Core.cs，名单漂移自愈。
$coreFiles = Get-ChildItem -Path "$RepoRoot/src" -Recurse -Filter "*Core.cs" -ErrorAction SilentlyContinue |
    Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' }
# P2-1 (review-2026-08-31)：显式 int-除法豁免清单（替代已被移除的"任一 ArgumentException"过宽豁免）——
# 下列文件经人工核实：除法均为整数除法（常量除数，不可能产生 NaN/Inf）。
# DateTimeCore：（Month+2)/3、(Month+5)/6、Easter 的 b/100 等；DoeCore：(r / div)、cc /= 3（数组索引）。
$intDivOnlyFiles = @('DateTimeCore.cs', 'DoeCore.cs')
$nanInfMissing = @()

foreach ($f in $coreFiles) {
    $content = Read-Utf8Text $f.FullName
    if (-not $content) { continue }
    # 剥离 // 与 /* */ 注释后再检测除法表达式：原正则会把 `/// <summary>` 等 XML 注释
    # 误判为除法，导致所有文件 hasDivision=true，守卫检查退化为「任一 ArgumentException 即豁免”。
    $code = [regex]::Replace($content, '/\*.*?\*/', '', [System.Text.RegularExpressions.RegexOptions]::Singleline)
    $code = [regex]::Replace($code, '(?m)//.*$', '')
    # P2-1：剥离字符串字面量——SqlCore 的 "DDL/DML" 等字符串里的斜杠被误判为除法。
    $code = [regex]::Replace($code, '"[^"]*"', '""')
    $hasDivision = $code -match '/\s*(?!0\b)\w+'
    # P2-1：int 除法豁免（显式名单，非"任一 ArgumentException"）
    if ($intDivOnlyFiles -contains $f.Name) { continue }
    if ($hasDivision) {
        # P2-1 (review-2026-08-31)：守卫判定改用已剥离注释的 $code——原用 $content，文件头写
        # 一句 `// ArgumentException 用于参数校验` 注释即可豁免整个文件的 NaN/Inf 检查；
        # 并移除 ArgumentException 豁免：参数校验异常与"除法结果的 NaN/Inf 显式守卫"无因果关系
        # （IEEE 除零不抛异常，静默产生 Inf）。
        $hasGuard = ($code -match 'double\.IsNaN') -or
                    ($code -match 'double\.IsInfinity') -or
                    ($code -match 'double\.NaN')
        if (-not $hasGuard) {
            $nanInfMissing += $f.FullName
            $violations += "NAN_INF_GUARD: $($f.FullName)"
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

# P1-18 (review-2026-08-31)：原 `-Filter "*Core.cs"` 只扫 *Core.cs，AnalyticsHelpers.cs（含
# ToDoubleMatrix(object[,])）等 Helper 漏网。改为排除 bin/obj 的全部 .cs——Udf 层方法接收
# object 单参（非 object[,] 直接参数），不会误匹配。
$allCoreCs = Get-ChildItem -Path "$RepoRoot/src" -Recurse -Filter "*.cs" -ErrorAction SilentlyContinue |
    Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' }
$hasHeaderViolations = @()
# Structural transformation exemptions (don't interpret header semantics)
# review-2026-08-29 P2-7：原 11 项中的 Frequency/Dict/JsonToTable/XmlToTable/RegexCaptureGroups
# 参数均为 object[]/string（非 object[,]），check 正则不匹配，属冗余项 → 收缩为与
# AGENTS.md §4 表头行契约一致的 6 项（Transpose/SelectColumns/SelectRows/CrossJoin/Flatten2D/Count）。
# 2026-08-29 发行前审查补充：DictSetCore.Keys/Values 为列提取（与 SelectColumns 同类结构变换）→ 豁免。
# P1-18 (review-2026-08-31)：AnalyticsHelpers.ToDoubleMatrix 是纯类型转换（无表头语义），登记豁免。
$structuralExempt = @('Transpose','SelectColumns','SelectRows','CrossJoin','Flatten2D','Count','Keys','Values','ToDoubleMatrix')

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
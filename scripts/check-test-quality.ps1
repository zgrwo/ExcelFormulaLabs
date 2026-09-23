#Requires -Version 5.1
<#
.SYNOPSIS
    ExcelFormulaLabs test quality gate.
.DESCRIPTION
    Scans tests/**/*.cs and blocks weak assertions:
      1. Zero-assertion test methods ([Fact]/[Theory] with no assertion call) - FAIL
      2. Tautological assertions (Assert.True(true) / Assert.False(false))    - FAIL
      3. Presence-only assertions (.Should().NotBeNull() / Assert.NotNull)    - WARN (budget)
    Expression-bodied tests (=> ...) and block-bodied tests are both scanned;
    comments/strings are skipped; custom helpers named Assert*/Check*/Verify*
    count as assertions (avoids false positives on AssertBb-style helpers).
    CrossValRunner (no xunit tests) and bin/obj are excluded.

    Budget semantics: presence-only tests are legitimate for "not null" contract
    checks but must not grow. -MaxWarn defaults to 0 (Phase 3 目标达成：预算归零)；
    exceeding the budget fails the gate.
.NOTES
    Usage: powershell -File scripts/check-test-quality.ps1 [-RepoRoot <path>] [-MaxWarn 0]
    自测：tests/scripts/test_check_test_quality.ps1（正向全绿 + 负向注入，CI 强制执行）
#>
param(
    [string]$RepoRoot = (Split-Path -Parent $PSScriptRoot),
    [int]$MaxWarn = 0
)

$ErrorActionPreference = "Stop"
$violations = @()
$warnings = @()

function Read-Utf8Text {
    param([string]$Path)
    if (-not (Test-Path $Path)) { return $null }
    return [System.IO.File]::ReadAllText($Path, [System.Text.Encoding]::UTF8)
}

# 跳过多行空白与注释（// 与 /* */），返回下一个有效字符的下标
function Skip-Trivia {
    param([string]$Text, [int]$Start)
    $i = $Start
    $n = $Text.Length
    while ($i -lt $n) {
        $c = $Text[$i]
        if ($c -eq ' ' -or $c -eq "`t" -or $c -eq "`r" -or $c -eq "`n") { $i++; continue }
        if ($c -eq '/' -and $i + 1 -lt $n -and $Text[$i + 1] -eq '/') {
            $k = $Text.IndexOf("`n", $i)
            $i = if ($k -lt 0) { $n } else { $k + 1 }
            continue
        }
        if ($c -eq '/' -and $i + 1 -lt $n -and $Text[$i + 1] -eq '*') {
            $k = $Text.IndexOf('*/', $i)
            $i = if ($k -lt 0) { $n } else { $k + 2 }
            continue
        }
        break
    }
    return $i
}

# 从参数表 ')' 之后提取方法体：块体 {} 或表达式体 => ...;
# 字符扫描跳过注释与字符串（含 @verbatim 字符串的双引号转义）。
function Get-MethodBody {
    param([string]$Text, [int]$AfterParams)
    $i = Skip-Trivia $Text $AfterParams
    $n = $Text.Length
    if ($i + 1 -lt $n -and $Text[$i] -eq '=' -and $Text[$i + 1] -eq '>') {
        # 表达式体：扫到顶层 ';'
        $j = $i + 2
        $depth = 0
        $inStr = [char]0
        $verbatim = $false
        while ($j -lt $n) {
            $c = $Text[$j]
            if ($inStr -ne [char]0) {
                if ($inStr -eq '"' -and $c -eq '\' -and -not $verbatim) { $j += 2; continue }
                if ($c -eq $inStr) {
                    if ($verbatim -and $j + 1 -lt $n -and $Text[$j + 1] -eq $inStr) { $j += 2; continue }
                    $inStr = [char]0
                }
                $j++; continue
            }
            if ($c -eq '/' -and $j + 1 -lt $n -and $Text[$j + 1] -eq '/') {
                $k = $Text.IndexOf("`n", $j); $j = if ($k -lt 0) { $n } else { $k }; continue
            }
            if ($c -eq '/' -and $j + 1 -lt $n -and $Text[$j + 1] -eq '*') {
                $k = $Text.IndexOf('*/', $j); $j = if ($k -lt 0) { $n } else { $k + 2 }; continue
            }
            if ($c -eq '"' -or $c -eq "'") {
                $verbatim = ($c -eq '"' -and $j -gt 0 -and $Text[$j - 1] -eq '@')
                $inStr = $c; $j++; continue
            }
            if ($c -eq '(' -or $c -eq '[' -or $c -eq '{') { $depth++ }
            elseif ($c -eq ')' -or $c -eq ']' -or $c -eq '}') { $depth-- }
            elseif ($c -eq ';' -and $depth -eq 0) { return $Text.Substring($i, $j - $i + 1) }
            $j++
        }
        return $Text.Substring($i)
    }
    if ($i -ge $n -or $Text[$i] -ne '{') { return $null }
    $j = $i
    $depth = 0
    $inStr = [char]0
    $verbatim = $false
    while ($j -lt $n) {
        $c = $Text[$j]
        if ($inStr -ne [char]0) {
            if ($inStr -eq '"' -and $c -eq '\' -and -not $verbatim) { $j += 2; continue }
            if ($c -eq $inStr) {
                if ($verbatim -and $j + 1 -lt $n -and $Text[$j + 1] -eq $inStr) { $j += 2; continue }
                $inStr = [char]0
            }
            $j++; continue
        }
        if ($c -eq '/' -and $j + 1 -lt $n -and $Text[$j + 1] -eq '/') {
            $k = $Text.IndexOf("`n", $j); $j = if ($k -lt 0) { $n } else { $k }; continue
        }
        if ($c -eq '/' -and $j + 1 -lt $n -and $Text[$j + 1] -eq '*') {
            $k = $Text.IndexOf('*/', $j); $j = if ($k -lt 0) { $n } else { $k + 2 }; continue
        }
        if ($c -eq '"' -or $c -eq "'") {
            $verbatim = ($c -eq '"' -and $j -gt 0 -and $Text[$j - 1] -eq '@')
            $inStr = $c; $j++; continue
        }
        if ($c -eq '{') { $depth++ }
        elseif ($c -eq '}') {
            $depth--
            if ($depth -eq 0) { return $Text.Substring($i, $j - $i + 1) }
        }
        $j++
    }
    return $null
}

# 剥离注释与字符串字面量后再做断言计数：块体提取保留原文，
# 注释里的 .Should() 与字符串里的 "Assert.X" 都不得冒充断言（自测场景 6）。
function Get-CodeOnly {
    param([string]$Text)
    $sb = New-Object System.Text.StringBuilder
    $i = 0
    $n = $Text.Length
    $inStr = [char]0
    $verbatim = $false
    while ($i -lt $n) {
        $c = $Text[$i]
        if ($inStr -ne [char]0) {
            if ($inStr -eq '"' -and $c -eq '\' -and -not $verbatim) { [void]$sb.Append('  '); $i += 2; continue }
            if ($c -eq $inStr) {
                if ($verbatim -and $i + 1 -lt $n -and $Text[$i + 1] -eq $inStr) { [void]$sb.Append('  '); $i += 2; continue }
                $inStr = [char]0
            }
            [void]$sb.Append(' ')
            $i++; continue
        }
        if ($c -eq '/' -and $i + 1 -lt $n -and $Text[$i + 1] -eq '/') {
            $k = $Text.IndexOf("`n", $i)
            if ($k -lt 0) { $k = $n }
            [void]$sb.Append(' ' * ($k - $i))
            $i = $k; continue
        }
        if ($c -eq '/' -and $i + 1 -lt $n -and $Text[$i + 1] -eq '*') {
            $k = $Text.IndexOf('*/', $i)
            if ($k -lt 0) { $k = $n } else { $k += 2 }
            [void]$sb.Append(' ' * ($k - $i))
            $i = $k; continue
        }
        if ($c -eq '"' -or $c -eq "'") {
            $verbatim = ($c -eq '"' -and $i -gt 0 -and $Text[$i - 1] -eq '@')
            $inStr = $c
        }
        [void]$sb.Append($c)
        $i++
    }
    return $sb.ToString()
}

$assertRx = '\.Should\s*\(|Assert\s*\.\s*\w+|\bAssert\w+\s*\(|\bCheck\w+\s*\(|\bVerify\w+\s*\(|Record\s*\.\s*Exception'
$presenceRx = '\.Should\s*\(\s*\)\s*\.\s*NotBeNull\s*\(|Assert\s*\.\s*NotNull\s*\('
$tautologyRx = 'Assert\s*\.\s*True\s*\(\s*true\s*\)|Assert\s*\.\s*False\s*\(\s*false\s*\)'
$testRx = '\[(Fact|Theory)\]\s*(?:\[[^\]]*\]\s*)*public\s+(?:async\s+)?(?:Task|void)\s+(\w+)\s*\('

Write-Host ""
Write-Host "[1/3] Checking zero-assertion tests ..."
$testFiles = Get-ChildItem -Path (Join-Path $RepoRoot "tests") -Recurse -Filter "*.cs" -ErrorAction SilentlyContinue |
    Where-Object { $_.FullName -notmatch "[/\\](bin|obj)[/\\]" -and $_.FullName -notmatch "[/\\]CrossValRunner[/\\]" }

$totalMethods = 0
foreach ($f in $testFiles) {
    $text = Read-Utf8Text $f.FullName
    if (-not $text) { continue }
    foreach ($m in [regex]::Matches($text, $testRx)) {
        $name = $m.Groups[2].Value
        # 跳到参数表匹配 ')'
        $k = $m.Index + $m.Length
        $depth = 1
        while ($k -lt $text.Length -and $depth -gt 0) {
            if ($text[$k] -eq '(') { $depth++ }
            elseif ($text[$k] -eq ')') { $depth-- }
            $k++
        }
        $body = Get-MethodBody $text $k
        if ($null -eq $body) { continue }
        $totalMethods++
        $code = Get-CodeOnly $body
        $assertCount = [regex]::Matches($code, $assertRx).Count
        $line = ($text.Substring(0, $m.Index) -split "`n").Count
        $rel = ($f.FullName.Substring($RepoRoot.Length) -replace '\\', '/').TrimStart('/')
        if ($assertCount -eq 0) {
            $violations += "ZERO_ASSERT: ${rel}:$line ($name)"
        }
        if ([regex]::IsMatch($code, $tautologyRx)) {
            $violations += "TAUTOLOGY: ${rel}:$line ($name)"
        }
    }
}
if ($violations.Count -gt 0) {
    Write-Host "  [FAIL] Found $($violations.Count) weak assertion violation(s)" -ForegroundColor Red
} else {
    Write-Host "  [OK] No zero-assertion / tautological tests ($totalMethods methods scanned)" -ForegroundColor Green
}

Write-Host ""
Write-Host "[2/3] Checking presence-only assertions (budget $MaxWarn) ..."
foreach ($f in $testFiles) {
    $text = Read-Utf8Text $f.FullName
    if (-not $text) { continue }
    foreach ($m in [regex]::Matches($text, $testRx)) {
        $name = $m.Groups[2].Value
        $k = $m.Index + $m.Length
        $depth = 1
        while ($k -lt $text.Length -and $depth -gt 0) {
            if ($text[$k] -eq '(') { $depth++ }
            elseif ($text[$k] -eq ')') { $depth-- }
            $k++
        }
        $body = Get-MethodBody $text $k
        if ($null -eq $body) { continue }
        $code = Get-CodeOnly $body
        $assertCount = [regex]::Matches($code, $assertRx).Count
        $presenceCount = [regex]::Matches($code, $presenceRx).Count
        if ($assertCount -gt 0 -and $assertCount -eq $presenceCount) {
            $line = ($text.Substring(0, $m.Index) -split "`n").Count
            $rel = ($f.FullName.Substring($RepoRoot.Length) -replace '\\', '/').TrimStart('/')
            $warnings += "PRESENCE_ONLY: ${rel}:$line ($name)"
        }
    }
}
if ($warnings.Count -gt $MaxWarn) {
    Write-Host "  [FAIL] Presence-only assertions $($warnings.Count) > budget $MaxWarn" -ForegroundColor Red
} else {
    Write-Host "  [OK] Presence-only assertions $($warnings.Count) <= budget $MaxWarn" -ForegroundColor Green
}

Write-Host ""
Write-Host "[3/3] Summary ..."
$blocked = ($violations.Count -gt 0) -or ($warnings.Count -gt $MaxWarn)
if ($blocked) {
    Write-Host "============================================================"
    Write-Host "  [BLOCKED] test quality violations:" -ForegroundColor Red
    foreach ($v in $violations) { Write-Host "    - $v" -ForegroundColor Red }
    if ($warnings.Count -gt $MaxWarn) {
        foreach ($w in $warnings) { Write-Host "    - $w" -ForegroundColor Red }
    }
    Write-Host "============================================================"
    exit 1
} else {
    Write-Host "  [PASS] Test quality checks passed." -ForegroundColor Green
    Write-Host "============================================================"
    exit 0
}
